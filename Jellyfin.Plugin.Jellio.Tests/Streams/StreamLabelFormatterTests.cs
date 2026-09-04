using System.Globalization;
using System.Threading;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellio.Streams;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Jellio.Tests.Streams;

public class StreamLabelFormatterTests
{
    [Theory]
    [InlineData(VideoRangeType.HDR10, "HDR10")]
    [InlineData(VideoRangeType.HDR10Plus, "HDR10+")]
    [InlineData(VideoRangeType.HLG, "HLG")]
    [InlineData(VideoRangeType.DOVI, "DV")]
    [InlineData(VideoRangeType.DOVIWithHDR10, "DV+HDR10")]
    [InlineData(VideoRangeType.DOVIWithHDR10Plus, "DV+HDR10+")]
    [InlineData(VideoRangeType.DOVIWithHLG, "DV+HLG")]
    [InlineData(VideoRangeType.DOVIWithSDR, "DV+SDR")]
    [InlineData(VideoRangeType.DOVIWithEL, "DV+EL")]
    [InlineData(VideoRangeType.DOVIWithELHDR10Plus, "DV+EL+HDR10+")]
    [InlineData(VideoRangeType.DOVIInvalid, "HDR")]
    [InlineData(VideoRangeType.Unknown, "HDR")]
    [InlineData((VideoRangeType)999, "HDR")]
    public void FormatVideoRange_MapsEnumToDisplayString(VideoRangeType input, string expected)
    {
        Assert.Equal(expected, StreamLabelFormatter.FormatVideoRange(input));
    }

    [Theory]
    [InlineData(29_500_000, "29.5 Mbps")]
    [InlineData(30_000_000, "30 Mbps")]
    [InlineData(null, null)]
    [InlineData(0, null)]
    public void FormatBitrate_RoundsAndOmitsMissing(int? bitrateBps, string? expected)
    {
        Assert.Equal(expected, StreamLabelFormatter.FormatBitrate(bitrateBps));
    }

    [Fact]
    public void FormatBitrate_UsesInvariantCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pl-PL");
            Assert.Equal("29.5 Mbps", StreamLabelFormatter.FormatBitrate(29_500_000));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void FormatDirectDescription_OmitsMissingOptionalFields()
    {
        var source = new MediaSourceInfo
        {
            Bitrate = null,
        };
        var videoStream = new MediaStream
        {
            Type = MediaStreamType.Video,
            Codec = "hevc",
            ColorTransfer = "smpte2084",
            Height = null,
        };

        var description = StreamLabelFormatter.FormatDirectDescription(source, videoStream, audioStream: null);

        Assert.Equal("Direct Play · HEVC HDR10", description);
    }

}
