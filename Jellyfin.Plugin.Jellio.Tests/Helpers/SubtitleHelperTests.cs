using System.Collections.Generic;
using Jellyfin.Plugin.Jellio.Helpers;
using Jellyfin.Plugin.Jellio.Models;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Jellio.Tests;

public class SubtitleHelperTests
{
    private const string BaseUrl = "http://jellyfin.local";
    private const string AuthToken = "test-token";

    private static BaseItemDto MakeItem(Guid id, string name, params MediaSourceInfo[] sources) => new()
    {
        Id = id,
        Name = name,
        MediaSources = sources,
    };

    private static MediaSourceInfo MakeSource(string id, params MediaStream[] streams) => new()
    {
        Id = id,
        MediaStreams = streams,
    };

    private static MediaStream MakeSubtitle(int index, string codec, string? language = "eng", bool isExternal = false) => new()
    {
        Type = MediaStreamType.Subtitle,
        Index = index,
        Codec = codec,
        Language = language,
        IsExternal = isExternal,
    };

    [Fact]
    public void BuildSubtitleDtos_ReturnsEmpty_WhenNoItems()
    {
        var result = SubtitleHelper.BuildSubtitleDtos(Array.Empty<BaseItemDto>(), BaseUrl, AuthToken);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildSubtitleDtos_ReturnsEmpty_WhenMediaSourcesIsNull()
    {
        var item = new BaseItemDto { Id = Guid.NewGuid(), Name = "Movie", MediaSources = null };

        var result = SubtitleHelper.BuildSubtitleDtos([item], BaseUrl, AuthToken);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildSubtitleDtos_ReturnsEmpty_WhenMediaStreamsIsNull()
    {
        var source = new MediaSourceInfo { Id = "src1", MediaStreams = null };
        var item = MakeItem(Guid.NewGuid(), "Movie", source);

        var result = SubtitleHelper.BuildSubtitleDtos([item], BaseUrl, AuthToken);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildSubtitleDtos_IncludesConvertibleTextSubtitle()
    {
        var itemId = Guid.NewGuid();
        var source = MakeSource("src1", MakeSubtitle(2, "srt"));
        var item = MakeItem(itemId, "Movie", source);

        var result = SubtitleHelper.BuildSubtitleDtos([item], BaseUrl, AuthToken);

        var subtitle = Assert.Single(result);
        Assert.Equal($"jelliopp-{itemId}-2", subtitle.Id);
        Assert.Equal("eng", subtitle.Lang);
        Assert.Equal($"{BaseUrl}/Videos/{itemId}/src1/Subtitles/2/0/Stream.srt?api_key={AuthToken}", subtitle.Url);
    }

    [Theory]
    [InlineData("ass")]
    [InlineData("ssa")]
    public void BuildSubtitleDtos_ExcludesFormatsThatCannotConvertToSrt(string codec)
    {
        // ASS/SSA carry styling/positioning info SRT has no room for; Jellyfin's own
        // SupportsSubtitleConversionTo refuses these regardless of target, so advertising
        // them here would produce a subtitle entry Jellyfin can't actually serve as SRT.
        var source = MakeSource("src1", MakeSubtitle(2, codec));
        var item = MakeItem(Guid.NewGuid(), "Movie", source);

        var result = SubtitleHelper.BuildSubtitleDtos([item], BaseUrl, AuthToken);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildSubtitleDtos_ExcludesImageBasedSubtitles()
    {
        // PGS (Blu-ray) subtitles are bitmap images, not text -- not a text subtitle
        // stream at all, so they can never be delivered as SRT.
        var source = MakeSource("src1", MakeSubtitle(2, "pgssub"));
        var item = MakeItem(Guid.NewGuid(), "Movie", source);

        var result = SubtitleHelper.BuildSubtitleDtos([item], BaseUrl, AuthToken);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildSubtitleDtos_ExcludesNonSubtitleStreams()
    {
        var videoStream = new MediaStream { Type = MediaStreamType.Video, Index = 0, Codec = "h264" };
        var audioStream = new MediaStream { Type = MediaStreamType.Audio, Index = 1, Codec = "aac" };
        var source = MakeSource("src1", videoStream, audioStream);
        var item = MakeItem(Guid.NewGuid(), "Movie", source);

        var result = SubtitleHelper.BuildSubtitleDtos([item], BaseUrl, AuthToken);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildSubtitleDtos_DefaultsToUnd_WhenLanguageMissing()
    {
        var source = MakeSource("src1", MakeSubtitle(2, "srt", language: null));
        var item = MakeItem(Guid.NewGuid(), "Movie", source);

        var result = SubtitleHelper.BuildSubtitleDtos([item], BaseUrl, AuthToken);

        var subtitle = Assert.Single(result);
        Assert.Equal("und", subtitle.Lang);
    }

    [Fact]
    public void BuildSubtitleDtos_HandlesMultipleItemsSourcesAndStreams()
    {
        var item1 = MakeItem(
            Guid.NewGuid(),
            "Episode 1",
            MakeSource("src1", MakeSubtitle(2, "srt", "eng"), MakeSubtitle(3, "subrip", "pol")));
        var item2 = MakeItem(
            Guid.NewGuid(),
            "Episode 2",
            MakeSource("src2", MakeSubtitle(2, "vtt", "spa")));

        var result = SubtitleHelper.BuildSubtitleDtos([item1, item2], BaseUrl, AuthToken);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.Lang == "eng");
        Assert.Contains(result, s => s.Lang == "pol");
        Assert.Contains(result, s => s.Lang == "spa");
    }

    [Fact]
    public void BuildSubtitleDtos_IncludesExternalTextSubtitleFiles()
    {
        // External .srt sidecar files (IsExternal=true) are a common case: no embedded
        // Codec is reported for some external subs, so this also guards the
        // "!IsExternal" branch inside Jellyfin's own IsTextSubtitleStream check.
        var externalStream = MakeSubtitle(0, "srt", "eng", isExternal: true);
        var source = MakeSource("src1", externalStream);
        var item = MakeItem(Guid.NewGuid(), "Movie", source);

        var result = SubtitleHelper.BuildSubtitleDtos([item], BaseUrl, AuthToken);

        Assert.Single(result);
    }
}
