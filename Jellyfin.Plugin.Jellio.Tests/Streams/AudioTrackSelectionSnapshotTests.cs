using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Jellio.Streams;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Plugin.Jellio.Tests.Streams;

/// <summary>
/// Tests <see cref="AudioTrackSelection"/> with <see cref="MediaSourceInfo"/> data from a Jellyfin probe.
/// </summary>
/// <remarks>
/// The fixtures in <c>Fixtures/MediaSourceInfo/</c> hold serialized Jellyfin probe output.
/// One has six audio tracks with no language tags. The other has two English audio tracks.
/// </remarks>
public class AudioTrackSelectionSnapshotTests
{
    [Fact]
    public void ForSource_SixTracksWithoutLanguageTags_OffersEveryTrack()
    {
        var choices = AudioTrackSelection.ForSource(LoadSnapshot("multi-audio-6-tracks"));

        Assert.Equal([2, 3, 4, 5, 6, 7], choices.Select(choice => choice.StreamIndex));
        Assert.Equal(
            [
                "Dolby TrueHD + Dolby Atmos - 7.1",
                "Dolby Digital - 5.1",
                "DTS - Stereo (1)",
                "DTS-HD MA - 5.1",
                "DTS - Stereo (2)",
                "DTS - Stereo (3)",
            ],
            choices.Select(choice => choice.Label));
    }

    [Fact]
    public void ForSource_TwoTracksInTheSameLanguage_LabelsThemApart()
    {
        var choices = AudioTrackSelection.ForSource(LoadSnapshot("multi-audio-2-tracks"));

        Assert.Equal(
            [
                "English - Dolby Digital Plus + Dolby Atmos - 5.1 - Default",
                "English - Dolby Digital+ - 5.1",
            ],
            choices.Select(choice => choice.Label));
    }

    private static MediaSourceInfo LoadSnapshot(string slug)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MediaSourceInfo", $"{slug}.json");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };
        return JsonSerializer.Deserialize<MediaSourceInfo>(File.ReadAllText(path), options)!;
    }
}
