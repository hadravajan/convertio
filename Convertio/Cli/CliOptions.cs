using Convertio.Conversion;

namespace Convertio.Cli;

public sealed class CliOptions
{
    public required IReadOnlyList<string> Inputs { get; init; }
    public required OutputImageFormat Format { get; init; }
    public int Quality { get; init; } = 80;
    public string? OutputDirectory { get; init; }
    public bool Recursive { get; init; }
    public bool Overwrite { get; init; }
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    public static readonly string[] SupportedInputExtensions =
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tiff", ".tif"
    };
}
