using System.Diagnostics;
using Convertio.Cli;
using Convertio.Conversion;
using Convertio.Logging;

namespace Convertio;

internal static class Program
{
    private static class ExitCode
    {
        public const int Success = 0;
        public const int PartialFailure = 1;
        public const int ArgumentError = 2;
        public const int Cancelled = 130; // conventional SIGINT exit code
    }

    private static async Task<int> Main(string[] args)
    {
        CliOptions options;
        try
        {
            options = ArgumentParser.Parse(args);
        }
        catch (HelpRequestedException)
        {
            Console.WriteLine(ArgumentParser.HelpText);
            return ExitCode.Success;
        }
        catch (CliArgumentException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(ArgumentParser.HelpText);
            return ExitCode.ArgumentError;
        }

        IReadOnlyList<string> files;
        try
        {
            files = InputFileResolver.Resolve(options.Inputs, options.Recursive);
        }
        catch (CliArgumentException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCode.ArgumentError;
        }

        if (files.Count == 0)
        {
            Console.Error.WriteLine(
                "Error: no supported image files found in the given input(s). " +
                $"Supported extensions: {string.Join(", ", CliOptions.SupportedInputExtensions)}");
            return ExitCode.ArgumentError;
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true; // let us finish in-flight writes cleanly instead of hard-killing the process
            Console.WriteLine();
            Console.WriteLine("Cancellation requested, finishing in-flight conversions...");
            cts.Cancel();
        };

        var reporter = new ConsoleReporter();
        var batchConverter = new BatchConverter(new ImageConverter(), reporter);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var (results, skipped) = await batchConverter.RunAsync(files, options, cts.Token).ConfigureAwait(false);
            stopwatch.Stop();

            reporter.PrintSummary(results, skipped, stopwatch.Elapsed);

            return results.Any(r => !r.Success) ? ExitCode.PartialFailure : ExitCode.Success;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            Console.WriteLine($"Cancelled after {stopwatch.Elapsed.TotalSeconds:0.00}s.");
            return ExitCode.Cancelled;
        }
    }
}
