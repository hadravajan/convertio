namespace Convertio.Conversion;

/// <summary>
/// Outcome of converting a single file. Immutable — built once by the converter
/// and only ever read afterwards, so it's safe to hand across threads/tasks.
/// </summary>
public sealed record ConversionResult
{
    public required string SourcePath { get; init; }
    public string? OutputPath { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public long OriginalSizeBytes { get; init; }
    public long OutputSizeBytes { get; init; }
    public TimeSpan Elapsed { get; init; }

    public double ReductionPercent =>
        OriginalSizeBytes <= 0
            ? 0
            : (1.0 - (double)OutputSizeBytes / OriginalSizeBytes) * 100.0;

    public static ConversionResult Failed(string sourcePath, string errorMessage, TimeSpan elapsed) => new()
    {
        SourcePath = sourcePath,
        Success = false,
        ErrorMessage = errorMessage,
        Elapsed = elapsed
    };

    public static ConversionResult Succeeded(
        string sourcePath,
        string outputPath,
        long originalSizeBytes,
        long outputSizeBytes,
        TimeSpan elapsed) => new()
    {
        SourcePath = sourcePath,
        OutputPath = outputPath,
        Success = true,
        OriginalSizeBytes = originalSizeBytes,
        OutputSizeBytes = outputSizeBytes,
        Elapsed = elapsed
    };
}
