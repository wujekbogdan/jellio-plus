using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Jellio.Streams;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Plugin.Jellio.Tests.Streams;

/// <summary>
/// Verifies <see cref="StreamEntryFactory"/> against <see cref="MediaSourceInfo"/> output captured from a real Jellyfin probe.
/// </summary>
/// <remarks>
/// The JSON fixtures under <c>Fixtures/MediaSourceInfo/</c> were captured from a running Jellyfin probe of actual test videos (HDR10, Dolby Vision, HLG, SDR HEVC, SDR H.264). Re-capture is only needed if Jellyfin's probe output for those files changes.
/// </remarks>
public class StreamEntrySnapshotTests
{
    private const string BaseUrl = "http://localhost:8096";
    private const string AuthToken = "test-token";
    private static readonly Guid ItemId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Theory]
    [InlineData("hdr10-hevc-4k", "Direct Play · HEVC HDR10 · 2160p · 1.3 Mbps · AAC stereo", "Adaptive · HDR10 HEVC 4K Sample - BlackClipping (2024)")]
    [InlineData("dv-hevc", "Direct Play · HEVC DV · 1920p · 31.1 Mbps · EAC3 5.1", "Adaptive · DV HEVC Sample - Landing (2024)")]
    public void Build_HdrSnapshot_EmitsDirectAndAutoEntries(string slug, string expectedDirectDescription, string expectedAutoDescription)
    {
        var entries = StreamEntryFactory.Build(BuildRequest(LoadSnapshot(slug)));

        Assert.Equal(2, entries.Count);
        Assert.Equal(expectedDirectDescription, entries[0].Description);
        Assert.Equal(expectedAutoDescription, entries[1].Description);
        Assert.Contains($"/Videos/{ItemId}/main.m3u8?", entries[0].Url, StringComparison.Ordinal);
        Assert.Contains($"/Videos/{ItemId}/master.m3u8?", entries[1].Url, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("hlg-hevc")]
    [InlineData("sdr-hevc-1080p")]
    [InlineData("sdr-h264-1080p")]
    public void Build_NonHdrSnapshot_EmitsSingleMasterEntry(string slug)
    {
        var entries = StreamEntryFactory.Build(BuildRequest(LoadSnapshot(slug)));

        var entry = Assert.Single(entries);
        Assert.Contains($"/Videos/{ItemId}/master.m3u8?", entry.Url, StringComparison.Ordinal);
    }

    private static StreamEntryRequest BuildRequest(MediaSourceInfo source) => new()
    {
        ItemId = ItemId,
        MediaSource = source,
        IsMultiMediaSource = false,
        SourceIndex = 1,
        BaseUrl = BaseUrl,
        AuthToken = AuthToken,
    };

    private static MediaSourceInfo LoadSnapshot(string slug)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MediaSourceInfo", $"{slug}.json");
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };
        return JsonSerializer.Deserialize<MediaSourceInfo>(json, options)!;
    }
}
