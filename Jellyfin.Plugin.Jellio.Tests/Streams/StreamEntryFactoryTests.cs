using System;
using System.IO;
using Jellyfin.Plugin.Jellio.Streams;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Jellio.Tests.Streams;

public class StreamEntryFactoryTests
{
    private const string BaseUrl = "http://localhost:8096";
    private const string AuthToken = "test-token";
    private static readonly Guid ItemId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly string FixturePath = Path.Combine(
        AppContext.BaseDirectory, "Fixtures", "Big_Buck_Bunny_360_10s_1MB.mp4");

    private const string FixtureFileName = "Big_Buck_Bunny_360_10s_1MB.mp4";
    private const string FixtureExpectedHash = "55e62a37d26c362d";

    [Fact]
    public void Build_SdrSource_SingleMediaSource_ReturnsOneMasterEntry()
    {
        var entries = StreamEntryFactory.Build(SdrRequest());

        var entry = Assert.Single(entries);
        Assert.Equal("Jellio++", entry.Name);
        Assert.Equal("Movie.1080p.mkv", entry.Description);
        Assert.Contains($"/Videos/{ItemId}/master.m3u8?", entry.Url, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_AlwaysSetsNotWebReadyTrue()
    {
        var entries = StreamEntryFactory.Build(SdrRequest());

        var entry = Assert.Single(entries);
        Assert.NotNull(entry.BehaviorHints);
        Assert.True(entry.BehaviorHints.NotWebReady);
    }

    [Fact]
    public void Build_PopulatesBehaviorHints_FromMediaSource()
    {
        var request = SdrRequestBuilder(source =>
        {
            source.Path = FixturePath;
            source.Size = 123_456L;
        });

        var entries = StreamEntryFactory.Build(request);

        var entry = Assert.Single(entries);
        Assert.NotNull(entry.BehaviorHints);
        Assert.Equal(FixtureFileName, entry.BehaviorHints.Filename);
        Assert.Equal(123_456L, entry.BehaviorHints.VideoSize);
        Assert.Equal(FixtureExpectedHash, entry.BehaviorHints.VideoHash);
    }

    [Fact]
    public void Build_HdrSourceWithDeclaredCodec_ReturnsTwoEntries_DirectFirstAutoSecond()
    {
        var entries = StreamEntryFactory.Build(HevcHdr10Request());

        Assert.Equal(2, entries.Count);
        Assert.Contains($"/Videos/{ItemId}/main.m3u8?", entries[0].Url, StringComparison.Ordinal);
        Assert.Contains($"/Videos/{ItemId}/master.m3u8?", entries[1].Url, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_HdrSourceWithDeclaredCodec_DirectEntryHasRichDescription()
    {
        var entries = StreamEntryFactory.Build(HevcHdr10Request());

        Assert.Equal("Direct Play · HEVC HDR10 · 2160p · 30 Mbps · EAC3 5.1", entries[0].Description);
    }

    [Fact]
    public void Build_HdrSourceSingleMediaSource_DirectEntryOmitsSourceNameSuffix()
    {
        var entries = StreamEntryFactory.Build(HevcHdr10Request());

        Assert.DoesNotContain(" — ", entries[0].Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_HdrSourceWithDeclaredCodec_AutoEntryDescriptionIsAdaptiveWithSourceName()
    {
        var entries = StreamEntryFactory.Build(HevcHdr10Request());

        Assert.Equal("Adaptive · Dune.HDR10.mkv", entries[1].Description);
    }

    [Fact]
    public void Build_HdrSourceWithUndeclaredCodec_ReturnsOneEntry()
    {
        var request = HevcHdr10RequestBuilder(source =>
        {
            source.MediaStreams =
            [
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = "vc1",
                    Height = 2160,
                    ColorTransfer = "smpte2084",
                },
            ];
        });

        var entries = StreamEntryFactory.Build(request);

        var entry = Assert.Single(entries);
        Assert.Contains($"/Videos/{ItemId}/master.m3u8?", entry.Url, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("audio-only")]
    [InlineData("empty")]
    [InlineData("null")]
    public void Build_SourceWithNoVideoStream_ReturnsOneEntry(string shape)
    {
        var request = SdrRequestBuilder(source =>
        {
            source.MediaStreams = shape switch
            {
                "audio-only" => [new MediaStream { Type = MediaStreamType.Audio, Codec = "aac" }],
                "empty" => [],
                _ => null,
            };
        });

        var entries = StreamEntryFactory.Build(request);

        var entry = Assert.Single(entries);
        Assert.Contains($"/Videos/{ItemId}/master.m3u8?", entry.Url, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_HdrSourceMultiMediaSource_AppendsFilenameSuffix()
    {
        var request = HevcHdr10RequestBuilder(source => source.Name = "Dune.HDR10.mkv") with
        {
            IsMultiMediaSource = true,
        };

        var entries = StreamEntryFactory.Build(request);

        Assert.EndsWith(" — Dune.HDR10.mkv", entries[0].Description, StringComparison.Ordinal);
        Assert.Equal("Adaptive · Dune.HDR10.mkv", entries[1].Description);
    }

    [Fact]
    public void Build_SdrSourceMultiMediaSource_DescriptionStillUsesSourceName()
    {
        var request = SdrRequest() with { IsMultiMediaSource = true };

        var entries = StreamEntryFactory.Build(request);

        var entry = Assert.Single(entries);
        Assert.Equal("Movie.1080p.mkv", entry.Description);
    }

    [Fact]
    public void Build_HdrSourceMultiMediaSource_NullSourceName_FallsBackToIndex()
    {
        var request = HevcHdr10RequestBuilder(source => source.Name = null) with
        {
            IsMultiMediaSource = true,
            SourceIndex = 2,
        };

        var entries = StreamEntryFactory.Build(request);

        Assert.EndsWith(" — #2", entries[0].Description, StringComparison.Ordinal);
        Assert.Equal("Adaptive · #2", entries[1].Description);
    }

    [Fact]
    public void Build_HdrSourceSingleMediaSource_NullSourceName_AutoIsBareAdaptive()
    {
        var request = HevcHdr10RequestBuilder(source => source.Name = null);

        var entries = StreamEntryFactory.Build(request);

        Assert.Equal("Adaptive", entries[1].Description);
    }

    [Fact]
    public void PickAudioStream_PrefersDefaultStream()
    {
        var source = new MediaSourceInfo
        {
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Audio, Codec = "aac", IsDefault = false },
                new MediaStream { Type = MediaStreamType.Audio, Codec = "eac3", IsDefault = true },
            ],
        };

        var picked = StreamEntryFactory.PickAudioStream(source);

        Assert.NotNull(picked);
        Assert.Equal("eac3", picked.Codec);
    }

    [Fact]
    public void PickAudioStream_FallsBackToFirstWhenNoDefault()
    {
        var source = new MediaSourceInfo
        {
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Audio, Codec = "aac", IsDefault = false },
                new MediaStream { Type = MediaStreamType.Audio, Codec = "eac3", IsDefault = false },
            ],
        };

        var picked = StreamEntryFactory.PickAudioStream(source);

        Assert.NotNull(picked);
        Assert.Equal("aac", picked.Codec);
    }

    [Fact]
    public void Build_SdrSource_NullSourceName_DescriptionIsEmpty()
    {
        var request = SdrRequestBuilder(source => source.Name = null);

        var entries = StreamEntryFactory.Build(request);

        var entry = Assert.Single(entries);
        Assert.Empty(entry.Description);
    }

    [Fact]
    public void Build_QueryString_IncludesAuthAndCodecLists()
    {
        var entries = StreamEntryFactory.Build(HevcHdr10Request());

        foreach (var entry in entries)
        {
            var query = entry.Url[entry.Url.IndexOf('?', StringComparison.Ordinal)..];
            Assert.Contains("mediaSourceId=source-hdr", query, StringComparison.Ordinal);
            Assert.Contains($"api_key={AuthToken}", query, StringComparison.Ordinal);
            Assert.Contains("videoCodec=h264,hevc,av1", query, StringComparison.Ordinal);
            Assert.Contains("audioCodec=aac,mp3,ac3,eac3,flac,opus", query, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Build_NullSourcePath_NullsFilenameAndHash()
    {
        var request = SdrRequestBuilder(source =>
        {
            source.Path = null;
            source.Size = 999L;
        });

        var entries = StreamEntryFactory.Build(request);

        var entry = Assert.Single(entries);
        Assert.NotNull(entry.BehaviorHints);
        Assert.Null(entry.BehaviorHints.Filename);
        Assert.Null(entry.BehaviorHints.VideoHash);
        Assert.Equal(999L, entry.BehaviorHints.VideoSize);
    }

    private static StreamEntryRequest SdrRequest() => SdrRequestBuilder(_ => { });

    private static StreamEntryRequest HevcHdr10Request() => HevcHdr10RequestBuilder(_ => { });

    private static StreamEntryRequest HevcHdr10RequestBuilder(Action<MediaSourceInfo> customize)
    {
        var source = new MediaSourceInfo
        {
            Id = "source-hdr",
            Name = "Dune.HDR10.mkv",
            Bitrate = 30_000_000,
            MediaStreams =
            [
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = "hevc",
                    Height = 2160,
                    ColorTransfer = "smpte2084",
                },
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = "eac3",
                    ChannelLayout = "5.1",
                    IsDefault = true,
                },
            ],
        };
        customize(source);
        return new StreamEntryRequest
        {
            ItemId = ItemId,
            MediaSource = source,
            IsMultiMediaSource = false,
            SourceIndex = 1,
            BaseUrl = BaseUrl,
            AuthToken = AuthToken,
        };
    }

    private static StreamEntryRequest SdrRequestBuilder(Action<MediaSourceInfo> customize)
    {
        var source = new MediaSourceInfo
        {
            Id = "source-1",
            Name = "Movie.1080p.mkv",
            MediaStreams =
            [
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = "h264",
                    Height = 1080,
                },
            ],
        };
        customize(source);
        return new StreamEntryRequest
        {
            ItemId = ItemId,
            MediaSource = source,
            IsMultiMediaSource = false,
            SourceIndex = 1,
            BaseUrl = BaseUrl,
            AuthToken = AuthToken,
        };
    }
}
