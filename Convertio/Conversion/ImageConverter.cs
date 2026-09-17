using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

namespace Convertio.Conversion;

/// <summary>
/// ImageSharp-backed converter. Stateless and thread-safe — one instance is shared
/// across the whole batch and called concurrently by the orchestrator.
/// </summary>
public sealed class ImageConverter : IImageConverter
{
    public async Task<ConversionResult> ConvertAsync(
        string sourcePath,
        string outputPath,
        OutputImageFormat format,
        int quality,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!File.Exists(sourcePath))
        {
            return ConversionResult.Failed(sourcePath, "Source file not found.", stopwatch.Elapsed);
        }

        long originalSize;
        try
        {
            originalSize = new FileInfo(sourcePath).Length;
        }
        catch (IOException ex)
        {
            return ConversionResult.Failed(sourcePath, $"Could not read source file: {ex.Message}", stopwatch.Elapsed);
        }

        try
        {
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using Image<Rgba32> image = await Image.LoadAsync<Rgba32>(sourcePath, cancellationToken)
                .ConfigureAwait(false);

            // JPEG has no alpha channel. Flattening explicitly onto white keeps output
            // deterministic instead of relying on the encoder's implicit alpha-drop
            // behaviour, which for some source formats can leave a black background.
            if (format == OutputImageFormat.Jpeg)
            {
                image.Mutate(ctx => ctx.BackgroundColor(Color.White));
            }

            IImageEncoder encoder = format switch
            {
                OutputImageFormat.Jpeg => new JpegEncoder { Quality = quality },
                OutputImageFormat.Webp => new WebpEncoder
                {
                    Quality = quality,
                    FileFormat = WebpFileFormatType.Lossy
                },
                OutputImageFormat.Png => BuildPngEncoder(quality),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported output format.")
            };

            // Write to a temp file first and rename into place so a crash or cancellation
            // mid-write never leaves a truncated/corrupt file at the final output path.
            var tempPath = outputPath + ".tmp";
            try
            {
                await using (var stream = File.Create(tempPath))
                {
                    await image.SaveAsync(stream, encoder, cancellationToken).ConfigureAwait(false);
                }

                File.Move(tempPath, outputPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }

            stopwatch.Stop();
            var outputSize = new FileInfo(outputPath).Length;
            return ConversionResult.Succeeded(sourcePath, outputPath, originalSize, outputSize, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            throw; // cancellation is not a per-file failure — let it propagate to the orchestrator
        }
        catch (UnknownImageFormatException ex)
        {
            return ConversionResult.Failed(sourcePath, $"Unrecognized or corrupt image format: {ex.Message}", stopwatch.Elapsed);
        }
        catch (InvalidImageContentException ex)
        {
            return ConversionResult.Failed(sourcePath, $"Invalid image content: {ex.Message}", stopwatch.Elapsed);
        }
        catch (NotSupportedException ex)
        {
            return ConversionResult.Failed(sourcePath, $"Unsupported image data: {ex.Message}", stopwatch.Elapsed);
        }
        catch (IOException ex)
        {
            return ConversionResult.Failed(sourcePath, $"I/O error: {ex.Message}", stopwatch.Elapsed);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ConversionResult.Failed(sourcePath, $"Access denied: {ex.Message}", stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// PNG is lossless by format spec, so "quality" can't mean compression artifacts the
    /// way it does for JPEG/WebP. The only real lever for size is color count: at quality
    /// 100 we keep full 32-bit truecolor; below that we quantize to an indexed palette
    /// whose size scales with quality (4-255 colors), trading fidelity for size the same
    /// way the other two formats trade it via their quality parameter.
    /// </summary>
    private static PngEncoder BuildPngEncoder(int quality)
    {
        if (quality >= 100)
        {
            return new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = PngColorType.RgbWithAlpha
            };
        }

        var maxColors = Math.Clamp((int)Math.Round(quality / 100.0 * 255), 4, 255);

        return new PngEncoder
        {
            CompressionLevel = PngCompressionLevel.BestCompression,
            ColorType = PngColorType.Palette,
            Quantizer = new WuQuantizer(new QuantizerOptions { MaxColors = maxColors })
        };
    }
}
