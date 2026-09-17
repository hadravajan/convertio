using Convertio.Conversion;
using Xunit;

namespace Convertio.Tests;

public class OutputPathBuilderTests
{
    [Fact]
    public void Build_DefaultsAlongsideSource_WhenNoOutputDirectoryGiven()
    {
        var path = OutputPathBuilder.Build(
            Path.Combine("in", "photo.png"), OutputImageFormat.Webp, outputDirectory: null);

        Assert.Equal(Path.Combine("in", "photo.webp"), path);
    }

    [Fact]
    public void Build_UsesGivenOutputDirectory()
    {
        var path = OutputPathBuilder.Build(
            Path.Combine("in", "photo.png"), OutputImageFormat.Jpeg, outputDirectory: "out");

        Assert.Equal(Path.Combine("out", "photo.jpg"), path);
    }

    [Fact]
    public void Build_StripsOriginalExtension_RegardlessOfSourceFormat()
    {
        var path = OutputPathBuilder.Build(
            Path.Combine("in", "photo.tiff"), OutputImageFormat.Png, outputDirectory: null);

        Assert.Equal(Path.Combine("in", "photo.png"), path);
    }

    [Fact]
    public void Build_DisambiguatesWhenOutputWouldOverwriteSource()
    {
        // Converting jpg -> jpg with no output directory would otherwise collide
        // with the source file itself and destroy it on write.
        var source = Path.Combine("in", "photo.jpg");
        var path = OutputPathBuilder.Build(source, OutputImageFormat.Jpeg, outputDirectory: null);

        Assert.Equal(Path.Combine("in", "photo.converted.jpg"), path);
        Assert.NotEqual(Path.GetFullPath(source), Path.GetFullPath(path));
    }

    [Fact]
    public void Build_DoesNotDisambiguate_WhenOutputDirectoryDiffersFromSource()
    {
        // Same format, but a different output directory means no collision risk.
        var path = OutputPathBuilder.Build(
            Path.Combine("in", "photo.jpg"), OutputImageFormat.Jpeg, outputDirectory: "out");

        Assert.Equal(Path.Combine("out", "photo.jpg"), path);
    }
}
