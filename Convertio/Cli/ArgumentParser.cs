using Convertio.Conversion;

namespace Convertio.Cli;

/// <summary>
/// Deliberately hand-rolled instead of pulling in System.CommandLine: at time of writing
/// that package is still pre-1.0 with breaking changes between previews, which is a bad
/// fit for a "production-ready" deliverable. This covers everything the CLI needs and is
/// trivial to unit test.
/// </summary>
public static class ArgumentParser
{
    public const string HelpText = """
        Convertio - batch image format converter and size optimizer

        USAGE:
          convertio <input...> -f <jpg|png|webp> [options]

        ARGUMENTS:
          <input...>              One or more image files and/or directories to convert.

        OPTIONS:
          -f, --format <fmt>      Output format: jpg, png, or webp. Required.
          -q, --quality <1-100>   Output quality. Default: 80.
                                  For jpg/webp this is standard lossy quality.
                                  For png (lossless format) this controls palette
                                  quantization: 100 = full truecolor, lower = fewer
                                  colors and a smaller file.
          -o, --output <dir>      Directory to write converted files into.
                                  Default: alongside each source file.
          -r, --recursive         When an input is a directory, descend into subdirectories.
          -y, --overwrite         Overwrite existing output files instead of skipping them.
          -p, --parallel <n>      Max concurrent conversions. Default: processor count.
          -h, --help              Show this help text.

        EXAMPLES:
          convertio photo.png -f webp -q 75
          convertio ./photos -f jpg -q 85 -o ./out -r
          convertio a.png b.bmp c.tiff -f webp -q 90 -y
        """;

    public static CliOptions Parse(string[] args)
    {
        if (args.Length == 0 || args.Any(a => a is "-h" or "--help"))
        {
            throw new HelpRequestedException();
        }

        var inputs = new List<string>();
        OutputImageFormat? format = null;
        int quality = 80;
        string? outputDirectory = null;
        bool recursive = false;
        bool overwrite = false;
        int parallelism = Environment.ProcessorCount;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "-f":
                case "--format":
                    {
                        var value = RequireValue(args, ref i, arg);
                        if (!OutputImageFormatExtensions.TryParse(value, out var parsed))
                        {
                            throw new CliArgumentException(
                                $"Unsupported format '{value}'. Supported formats: jpg, png, webp.");
                        }
                        format = parsed;
                        break;
                    }

                case "-q":
                case "--quality":
                    {
                        var value = RequireValue(args, ref i, arg);
                        if (!int.TryParse(value, out quality) || quality < 1 || quality > 100)
                        {
                            throw new CliArgumentException(
                                $"Quality must be an integer between 1 and 100. Got '{value}'.");
                        }
                        break;
                    }

                case "-o":
                case "--output":
                    outputDirectory = RequireValue(args, ref i, arg);
                    break;

                case "-r":
                case "--recursive":
                    recursive = true;
                    break;

                case "-y":
                case "--overwrite":
                    overwrite = true;
                    break;

                case "-p":
                case "--parallel":
                    {
                        var value = RequireValue(args, ref i, arg);
                        if (!int.TryParse(value, out parallelism) || parallelism < 1)
                        {
                            throw new CliArgumentException(
                                $"--parallel must be a positive integer. Got '{value}'.");
                        }
                        break;
                    }

                default:
                    if (arg.StartsWith('-'))
                    {
                        throw new CliArgumentException($"Unrecognized option '{arg}'.");
                    }
                    inputs.Add(arg);
                    break;
            }
        }

        if (inputs.Count == 0)
        {
            throw new CliArgumentException("At least one input file or directory is required.");
        }

        if (format is null)
        {
            throw new CliArgumentException("Output format is required. Pass -f/--format jpg|png|webp.");
        }

        return new CliOptions
        {
            Inputs = inputs,
            Format = format.Value,
            Quality = quality,
            OutputDirectory = outputDirectory,
            Recursive = recursive,
            Overwrite = overwrite,
            MaxDegreeOfParallelism = parallelism
        };
    }

    private static string RequireValue(string[] args, ref int index, string flag)
    {
        if (index + 1 >= args.Length)
        {
            throw new CliArgumentException($"Option '{flag}' requires a value.");
        }
        index++;
        return args[index];
    }
}

/// <summary>Signals that -h/--help was requested (or no args given) — not an error.</summary>
public sealed class HelpRequestedException : Exception;
