namespace Convertio.Conversion;

/// <summary>
/// Supported output formats. Deliberately a closed set — the CLI validates
/// against this instead of trusting a free-text extension.
/// </summary>
public enum OutputImageFormat
{
    Jpeg,
    Png,
    Webp
}

public static class OutputImageFormatExtensions
{
    public static string ToExtension(this OutputImageFormat format) => format switch
    {
        OutputImageFormat.Jpeg => ".jpg",
        OutputImageFormat.Png => ".png",
        OutputImageFormat.Webp => ".webp",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported output format.")
    };

    public static bool TryParse(string value, out OutputImageFormat format)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "jpg":
            case "jpeg":
                format = OutputImageFormat.Jpeg;
                return true;
            case "png":
                format = OutputImageFormat.Png;
                return true;
            case "webp":
                format = OutputImageFormat.Webp;
                return true;
            default:
                format = default;
                return false;
        }
    }
}
