using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Jellio.Streams;

/// <summary>
/// Returns the audio tracks that a media source offers as separate streams.
/// Jellyfin muxes only one audio track into an HLS stream.
/// A source with more tracks needs one stream per track.
/// </summary>
/// <remarks>
/// Returns an empty list when the source has fewer than two audio tracks, because there is no choice to offer.
/// The tracks keep the order of the media source.
/// Jellyfin serves the first audio track when a request does not specify one.
/// </remarks>
public static class AudioTrackSelection
{
    private const int TracksNeededForAChoice = 2;

    public static IReadOnlyList<AudioTrackChoice> ForSource(MediaSourceInfo source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var audioStreams = (source.MediaStreams ?? [])
            .Where(stream => stream.Type == MediaStreamType.Audio)
            .ToList();

        return audioStreams.Count < TracksNeededForAChoice
            ? []
            : audioStreams
                .Select((stream, position) => new AudioTrackChoice
                {
                    StreamIndex = stream.Index,
                    Label = BuildLabel(stream, audioStreams, position),
                })
                .ToList();
    }

    private static string BuildLabel(MediaStream stream, IReadOnlyList<MediaStream> audioStreams, int position)
    {
        bool SharesLabel(MediaStream other) =>
            string.Equals(other.DisplayTitle, stream.DisplayTitle, StringComparison.Ordinal);

        if (audioStreams.Count(SharesLabel) == 1)
        {
            return stream.DisplayTitle;
        }

        var ordinal = audioStreams.Take(position + 1).Count(SharesLabel);
        return $"{stream.DisplayTitle} ({ordinal.ToString(CultureInfo.InvariantCulture)})";
    }
}
