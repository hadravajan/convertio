using Convertio.Conversion;
using Xunit;

namespace Convertio.Tests;

public class OutputImageFormatTests
{
    [Theory]
    [InlineData("jpg", OutputImageFormat.Jpeg)]
    [InlineData("JPG", OutputImageFormat.Jpeg)]
    [InlineData("jpeg", OutputImageFormat.Jpeg)]
    [InlineData("png", OutputImageFormat.Png)]
    [InlineData("PNG", OutputImageFormat.Png)]
    [InlineData("webp", OutputImageFormat.Webp)]
    [InlineData(" webp ", OutputImageFormat.Webp)]
    public void TryParse_AcceptsKnownFormats(string input, OutputImageFormat expected)
    {
        Assert.True(OutputImageFormatExtensions.TryParse(input, out var parsed));
        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData("gif")]
    [InlineData("bmp")]
    [InlineData("")]
    [InlineData("jpg2")]
    public void TryParse_RejectsUnknownFormats(string input)
    {
        Assert.False(OutputImageFormatExtensions.TryParse(input, out _));
    }

    [Theory]
    [InlineData(OutputImageFormat.Jpeg, ".jpg")]
    [InlineData(OutputImageFormat.Png, ".png")]
    [InlineData(OutputImageFormat.Webp, ".webp")]
    public void ToExtension_ReturnsExpectedExtension(OutputImageFormat format, string expected)
    {
        Assert.Equal(expected, format.ToExtension());
    }
}
