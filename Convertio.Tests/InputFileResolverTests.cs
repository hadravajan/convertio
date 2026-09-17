using Convertio.Cli;
using Xunit;

namespace Convertio.Tests;

public class InputFileResolverTests : IDisposable
{
    private readonly DirectoryInfo _tempDir = Directory.CreateTempSubdirectory("convertio_tests_");

    [Fact]
    public void Resolve_SingleFile_ReturnsThatFile()
    {
        var file = WriteFile("a.png");
        var result = InputFileResolver.Resolve([file], recursive: false);

        Assert.Single(result);
        Assert.Equal(Path.GetFullPath(file), result[0]);
    }

    [Fact]
    public void Resolve_Directory_NonRecursive_SkipsSubdirectories()
    {
        WriteFile("a.png");
        WriteFile("b.jpg");
        WriteFile("sub/c.webp", inSubdir: true);

        var result = InputFileResolver.Resolve([_tempDir.FullName], recursive: false);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Resolve_Directory_Recursive_IncludesSubdirectories()
    {
        WriteFile("a.png");
        WriteFile("b.jpg");
        WriteFile("sub/c.webp", inSubdir: true);

        var result = InputFileResolver.Resolve([_tempDir.FullName], recursive: true);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Resolve_ExcludesUnsupportedExtensions()
    {
        WriteFile("a.png");
        WriteFile("notes.txt");
        WriteFile("archive.zip");

        var result = InputFileResolver.Resolve([_tempDir.FullName], recursive: false);

        Assert.Single(result);
        Assert.EndsWith("a.png", result[0]);
    }

    [Fact]
    public void Resolve_DeduplicatesOverlappingInputs()
    {
        var file = WriteFile("a.png");

        var result = InputFileResolver.Resolve([_tempDir.FullName, file], recursive: false);

        Assert.Single(result);
    }

    [Fact]
    public void Resolve_MissingPath_ThrowsCliArgumentException()
    {
        var missing = Path.Combine(_tempDir.FullName, "does-not-exist");
        Assert.Throws<CliArgumentException>(() => InputFileResolver.Resolve([missing], recursive: false));
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.png")]
    [InlineData("photo.webp")]
    [InlineData("photo.bmp")]
    [InlineData("photo.gif")]
    [InlineData("photo.tiff")]
    [InlineData("photo.tif")]
    public void Resolve_AcceptsAllSupportedExtensions(string fileName)
    {
        WriteFile(fileName);
        var result = InputFileResolver.Resolve([_tempDir.FullName], recursive: false);
        Assert.Single(result);
    }

    private string WriteFile(string relativePath, bool inSubdir = false)
    {
        var fullPath = Path.Combine(_tempDir.FullName, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(fullPath, "placeholder");
        return fullPath;
    }

    public void Dispose()
    {
        _tempDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }
}
