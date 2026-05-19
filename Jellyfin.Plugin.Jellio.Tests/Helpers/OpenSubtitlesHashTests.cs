using System.IO;
using Jellyfin.Plugin.Jellio.Helpers;

namespace Jellyfin.Plugin.Jellio.Tests;

public class OpenSubtitlesHashTests
{
    private const int ChunkSize = 65536;
    private const int MinSize = ChunkSize * 2;

    private static readonly string FixturePath = Path.Combine(
        AppContext.BaseDirectory, "Fixtures", "Big_Buck_Bunny_360_10s_1MB.mp4");

    private static MemoryStream CreateStream(int size) => new(new byte[size]);

    [Fact]
    public void ComputeFromPath_RealVideo_MatchesPythonReference()
    {
        // Hash computed via Python reference implementation (oshash algorithm)
        var referenseHash = "55e62a37d26c362d";
        Assert.Equal(referenseHash, OpenSubtitlesHash.ComputeFromPath(FixturePath));
    }

    [Fact]
    public void ComputeFromPath_ReturnsNull_WhenPathIsNull()
    {
        Assert.Null(OpenSubtitlesHash.ComputeFromPath(null));
    }

    [Fact]
    public void ComputeFromPath_ReturnsNull_WhenPathIsEmpty()
    {
        Assert.Null(OpenSubtitlesHash.ComputeFromPath(""));
    }

    [Fact]
    public void ComputeFromPath_ReturnsNull_WhenFileDoesNotExist()
    {
        Assert.Null(OpenSubtitlesHash.ComputeFromPath("/nonexistent/file.mkv"));
    }

    [Theory]
    [InlineData(1000, null)]
    [InlineData(MinSize - 1, null)]
    [InlineData(MinSize, "0000000000020000")]
    [InlineData(MinSize + 1, "0000000000020001")]
    public void Compute_BoundarySizes(int size, string? expected)
    {
        using var stream = CreateStream(size);
        Assert.Equal(expected, OpenSubtitlesHash.Compute(stream));
    }

    [Fact]
    public void Compute_Returns16CharLowercaseHexString()
    {
        using var stream = CreateStream(MinSize);
        var result = OpenSubtitlesHash.Compute(stream);

        Assert.NotNull(result);
        Assert.Equal(16, result.Length);
        Assert.Matches("^[0-9a-f]{16}$", result);
    }
}
