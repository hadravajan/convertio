using Convertio.Cli;
using Convertio.Conversion;
using Xunit;

namespace Convertio.Tests;

public class ArgumentParserTests
{
    [Fact]
    public void Parse_ReadsFormatAndQuality()
    {
        var options = ArgumentParser.Parse(["photo.png", "-f", "webp", "-q", "90"]);

        Assert.Equal(OutputImageFormat.Webp, options.Format);
        Assert.Equal(90, options.Quality);
        Assert.Single(options.Inputs);
    }

    [Fact]
    public void Parse_DefaultsQualityTo80()
    {
        var options = ArgumentParser.Parse(["a.png", "-f", "jpg"]);
        Assert.Equal(80, options.Quality);
    }

    [Fact]
    public void Parse_ReadsFlagsAndOutputDirectory()
    {
        var options = ArgumentParser.Parse(["a.png", "-f", "jpg", "-r", "-y", "-o", "out"]);

        Assert.True(options.Recursive);
        Assert.True(options.Overwrite);
        Assert.Equal("out", options.OutputDirectory);
    }

    [Fact]
    public void Parse_AcceptsMultiplePositionalInputs()
    {
        var options = ArgumentParser.Parse(["a.png", "b.png", "c.jpg", "-f", "jpg"]);
        Assert.Equal(3, options.Inputs.Count);
    }

    [Fact]
    public void Parse_ParsesLongFormFlags()
    {
        var options = ArgumentParser.Parse(
            ["a.png", "--format", "png", "--quality", "50", "--recursive", "--overwrite", "--output", "dst"]);

        Assert.Equal(OutputImageFormat.Png, options.Format);
        Assert.Equal(50, options.Quality);
        Assert.True(options.Recursive);
        Assert.True(options.Overwrite);
        Assert.Equal("dst", options.OutputDirectory);
    }

    [Fact]
    public void Parse_ThrowsCliArgumentException_WhenFormatMissing()
        => Assert.Throws<CliArgumentException>(() => ArgumentParser.Parse(["a.png"]));

    [Fact]
    public void Parse_ThrowsCliArgumentException_WhenFormatUnsupported()
        => Assert.Throws<CliArgumentException>(() => ArgumentParser.Parse(["a.png", "-f", "gif"]));

    [Fact]
    public void Parse_ThrowsCliArgumentException_WhenQualityTooLow()
        => Assert.Throws<CliArgumentException>(() => ArgumentParser.Parse(["a.png", "-f", "jpg", "-q", "0"]));

    [Fact]
    public void Parse_ThrowsCliArgumentException_WhenQualityTooHigh()
        => Assert.Throws<CliArgumentException>(() => ArgumentParser.Parse(["a.png", "-f", "jpg", "-q", "101"]));

    [Fact]
    public void Parse_ThrowsCliArgumentException_WhenQualityNotNumeric()
        => Assert.Throws<CliArgumentException>(() => ArgumentParser.Parse(["a.png", "-f", "jpg", "-q", "abc"]));

    [Fact]
    public void Parse_ThrowsCliArgumentException_ForUnknownFlag()
        => Assert.Throws<CliArgumentException>(() => ArgumentParser.Parse(["a.png", "-f", "jpg", "--bogus"]));

    [Fact]
    public void Parse_ThrowsCliArgumentException_WhenNoInputsGiven()
        => Assert.Throws<CliArgumentException>(() => ArgumentParser.Parse(["-f", "jpg"]));

    [Fact]
    public void Parse_ThrowsCliArgumentException_WhenFlagMissingItsValue()
        => Assert.Throws<CliArgumentException>(() => ArgumentParser.Parse(["a.png", "-f"]));

    [Fact]
    public void Parse_NoArgs_ThrowsHelpRequested()
    {
        Assert.Throws<HelpRequestedException>(() => ArgumentParser.Parse([]));
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public void Parse_HelpFlag_ThrowsHelpRequested_EvenAmongOtherArgs(string helpFlag)
    {
        Assert.Throws<HelpRequestedException>(() => ArgumentParser.Parse(["a.png", "-f", "jpg", helpFlag]));
    }
}
