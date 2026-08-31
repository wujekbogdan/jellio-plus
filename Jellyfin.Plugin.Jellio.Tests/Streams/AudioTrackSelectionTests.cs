using System.Linq;
using Jellyfin.Plugin.Jellio.Streams;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Jellio.Tests.Streams;

public class AudioTrackSelectionTests
{
    [Fact]
    public void ForSource_TwoAudioTracks_ReturnsOneChoicePerTrack()
    {
        var source = SourceWith(
            Audio(index: 1, codec: "eac3", channelLayout: "5.1"),
            Audio(index: 2, codec: "ac3", channelLayout: "stereo"));

        var choices = AudioTrackSelection.ForSource(source);

        Assert.Collection(
            choices,
            first => Assert.Equal(1, first.StreamIndex),
            second => Assert.Equal(2, second.StreamIndex));
    }

    [Fact]
    public void ForSource_SingleAudioTrack_ReturnsNoChoice()
    {
        var source = SourceWith(Video(index: 0), Audio(index: 1));

        Assert.Empty(AudioTrackSelection.ForSource(source));
    }

    [Fact]
    public void ForSource_LabelsTracksTheWayJellyfinDoes()
    {
        var source = SourceWith(
            Audio(index: 1, codec: "eac3", language: "eng", channelLayout: "5.1", isDefault: true),
            Audio(index: 2, codec: "dts", language: "pol", channelLayout: "stereo"));

        var choices = AudioTrackSelection.ForSource(source);

        Assert.Equal(
            ["English - Dolby Digital+ - 5.1 - Default", "Polish - DTS - Stereo"],
            choices.Select(choice => choice.Label));
    }

    /// <remarks>
    /// A disc rip can carry several commentary tracks with the same codec, channel layout and no language.
    /// Jellyfin calculates the same <c>DisplayTitle</c> for each one, which leaves the user with identical entries.
    /// </remarks>
    [Fact]
    public void ForSource_TracksWithTheSameLabel_NumbersEveryTrackInTheGroup()
    {
        var source = SourceWith(
            Audio(index: 2, codec: "truehd", channelLayout: "7.1"),
            Audio(index: 4, codec: "dts", channelLayout: "stereo"),
            Audio(index: 6, codec: "dts", channelLayout: "stereo"),
            Audio(index: 7, codec: "dts", channelLayout: "stereo"));

        var choices = AudioTrackSelection.ForSource(source);

        Assert.Equal(
            ["TRUEHD - 7.1", "DTS - Stereo (1)", "DTS - Stereo (2)", "DTS - Stereo (3)"],
            choices.Select(choice => choice.Label));
    }

    /// <remarks>
    /// Audio <c>MediaStream.Index</c> values are not contiguous and do not always start at 0.
    /// Code that re-indexes them selects the wrong track.
    /// </remarks>
    [Fact]
    public void ForSource_AudioIndexesStartAfterOtherStreams_PassesJellyfinIndexThroughUnchanged()
    {
        var source = SourceWith(
            Video(index: 0),
            Video(index: 1),
            Audio(index: 2, codec: "truehd"),
            Audio(index: 3, codec: "ac3"),
            Audio(index: 7, codec: "dts"));

        var choices = AudioTrackSelection.ForSource(source);

        Assert.Equal([2, 3, 7], choices.Select(choice => choice.StreamIndex));
    }

    private static MediaSourceInfo SourceWith(params MediaStream[] streams) =>
        new() { Id = "source-id", Name = "Movie.2160p.mkv", MediaStreams = streams };

    private static MediaStream Video(int index) =>
        new() { Type = MediaStreamType.Video, Index = index, Codec = "hevc" };

    private static MediaStream Audio(
        int index,
        string? codec = "eac3",
        string? language = null,
        string? channelLayout = null,
        bool isDefault = false) =>
        new()
        {
            Type = MediaStreamType.Audio,
            Index = index,
            Codec = codec,
            Language = language,
            ChannelLayout = channelLayout,
            IsDefault = isDefault,
        };
}
