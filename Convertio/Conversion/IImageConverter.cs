namespace Convertio.Conversion;

public interface IImageConverter
{
    /// <summary>
    /// Converts a single image file to the requested format/quality and writes it to <paramref name="outputPath"/>.
    /// Never throws for expected failure modes (corrupt/unsupported input, IO errors) — those are reported
    /// via <see cref="ConversionResult.Success"/> so a batch run can continue past bad files.
    /// </summary>
    Task<ConversionResult> ConvertAsync(
        string sourcePath,
        string outputPath,
        OutputImageFormat format,
        int quality,
        CancellationToken cancellationToken);
}
