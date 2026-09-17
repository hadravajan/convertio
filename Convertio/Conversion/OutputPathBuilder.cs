namespace Convertio.Conversion;

/// <summary>
/// Pulled out of BatchConverter as its own static class purely so it's directly unit
/// testable without spinning up the whole batch pipeline.
/// </summary>
public static class OutputPathBuilder
{
    public static string Build(string sourceFile, OutputImageFormat format, string? outputDirectory)
    {
        var fileNameNoExt = Path.GetFileNameWithoutExtension(sourceFile);
        var extension = format.ToExtension();
        var targetDir = string.IsNullOrEmpty(outputDirectory)
            ? Path.GetDirectoryName(sourceFile) ?? "."
            : outputDirectory;

        var candidate = Path.Combine(targetDir, fileNameNoExt + extension);

        // Converting a file to the same format it's already in (e.g. jpg -> jpg) would
        // otherwise overwrite the source itself. Disambiguate instead of destroying input.
        if (string.Equals(Path.GetFullPath(candidate), Path.GetFullPath(sourceFile), StringComparison.OrdinalIgnoreCase))
        {
            candidate = Path.Combine(targetDir, fileNameNoExt + ".converted" + extension);
        }

        return candidate;
    }
}
