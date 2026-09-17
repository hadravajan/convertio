namespace Convertio.Cli;

/// <summary>
/// Expands the user's raw input arguments (files and/or directories) into a concrete,
/// de-duplicated list of files to convert.
/// </summary>
public static class InputFileResolver
{
    public static IReadOnlyList<string> Resolve(IReadOnlyList<string> inputs, bool recursive)
    {
        var resolved = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var input in inputs)
        {
            if (File.Exists(input))
            {
                AddIfNew(input);
                continue;
            }

            if (Directory.Exists(input))
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.EnumerateFiles(input, "*", searchOption)
                    .Where(f => CliOptions.SupportedInputExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    AddIfNew(file);
                }
                continue;
            }

            throw new CliArgumentException($"Input path not found: '{input}'.");
        }

        return resolved;

        void AddIfNew(string path)
        {
            var full = Path.GetFullPath(path);
            if (seen.Add(full))
            {
                resolved.Add(full);
            }
        }
    }
}
