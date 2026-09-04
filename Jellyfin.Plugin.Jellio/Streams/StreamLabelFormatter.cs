using System.Globalization;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Jellio.Streams;

/// <summary>
/// Formats Jellyfin MediaSource metadata into description strings for stream entries. Output is locale-stable (numbers use <see cref="CultureInfo.InvariantCulture"/>); missing or zero-valued fields are dropped rather than replaced with placeholders, and unmapped <see cref="VideoRangeType"/> values render as the literal "HDR".
/// </summary>
internal static class StreamLabelFormatter
{
    private const string DirectPrefix = "Direct Play";
    private const string SegmentSeparator = " · ";

    internal static string FormatDirectDescription(MediaSourceInfo source, MediaStream videoStream, MediaStream? audioStream)
    {
        var videoCodec = ToUpperOrNull(videoStream.Codec);
        var videoRange = FormatVideoRange(videoStream.VideoRangeType);
        var resolution = videoStream.Height is int height && height > 0
            ? height.ToString(CultureInfo.InvariantCulture) + "p"
            : null;
        var bitrate = FormatBitrate(source.Bitrate);
        var audioCodec = ToUpperOrNull(audioStream?.Codec);
        var channels = NullIfEmpty(audioStream?.ChannelLayout);

        string?[] segments =
        [
            DirectPrefix,
            JoinNonEmpty(' ', videoCodec, videoRange),
            resolution,
            bitrate,
            JoinNonEmpty(' ', audioCodec, channels),
        ];

        return string.Join(SegmentSeparator, segments.Where(segment => !string.IsNullOrEmpty(segment)));
    }

    internal static string? FormatVideoRange(VideoRangeType type) => type switch
    {
        VideoRangeType.HDR10 => "HDR10",
        VideoRangeType.HDR10Plus => "HDR10+",
        VideoRangeType.HLG => "HLG",
        VideoRangeType.DOVI => "DV",
        VideoRangeType.DOVIWithHDR10 => "DV+HDR10",
        VideoRangeType.DOVIWithHDR10Plus => "DV+HDR10+",
        VideoRangeType.DOVIWithHLG => "DV+HLG",
        VideoRangeType.DOVIWithSDR => "DV+SDR",
        VideoRangeType.DOVIWithEL => "DV+EL",
        VideoRangeType.DOVIWithELHDR10Plus => "DV+EL+HDR10+",
        _ => "HDR",
    };

    internal static string? FormatBitrate(int? bitrateBps) =>
        bitrateBps is int bps && bps > 0
            ? (bps / 1_000_000.0).ToString("0.#", CultureInfo.InvariantCulture) + " Mbps"
            : null;

    private static string? ToUpperOrNull(string? value) =>
        string.IsNullOrEmpty(value) ? null : value.ToUpperInvariant();

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;

    private static string JoinNonEmpty(char separator, params string?[] parts) =>
        string.Join(separator, parts.Where(part => !string.IsNullOrEmpty(part)));
}
