using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Jellio.Helpers;

/// <summary>
/// Builds Stremio subtitle entries from Jellyfin media items.
/// </summary>
/// <remarks>
/// Kept separate from AddonController so the subtitle-building logic can be unit
/// tested without standing up any Jellyfin server dependencies (user/library managers,
/// DTO service, HTTP context) - the controller only resolves items/users and delegates here.
/// </remarks>
public static class SubtitleHelper
{
    /// <summary>
    /// Jellyfin serves subtitle tracks as SRT via the Stream.srt endpoint
    /// (see BuildSubtitleUrl), so only tracks Jellyfin can actually
    /// convert to SRT are worth exposing.
    /// </summary>
    private const string TargetSubtitleCodec = "srt";

    /// <summary>
    /// Builds the list of Stremio subtitle entries for the given items.
    /// </summary>
    /// <param name="items">The resolved Jellyfin items (already scoped to the requesting user).</param>
    /// <param name="baseUrl">The Jellyfin base URL to build subtitle stream URLs against.</param>
    /// <param name="authToken">The Jellyfin API key to authenticate the subtitle stream request.</param>
    /// <returns>The subtitle entries, in Stremio's expected shape.</returns>
    public static List<Models.SubtitleDto> BuildSubtitleDtos(
        IReadOnlyList<BaseItemDto> items,
        string baseUrl,
        string authToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(baseUrl);
        ArgumentNullException.ThrowIfNull(authToken);

        var subtitles = new List<Models.SubtitleDto>();

        foreach (var dto in items)
        {
            if (dto.MediaSources == null)
            {
                continue;
            }

            foreach (var source in dto.MediaSources)
            {
                if (source.MediaStreams == null)
                {
                    continue;
                }

                foreach (var stream in source.MediaStreams)
                {
                    if (!IsConvertibleSubtitle(stream))
                    {
                        continue;
                    }

                    var lang = string.IsNullOrEmpty(stream.Language) ? "und" : stream.Language;
                    var url = BuildSubtitleUrl(baseUrl, dto.Id, source.Id, stream.Index, authToken);
                    subtitles.Add(new Models.SubtitleDto
                    {
                        Id = $"jelliopp-{dto.Id}-{stream.Index}",
                        Url = url,
                        Lang = lang,
                    });
                    LogBuffer.AddLog($"[Subtitles] {dto.Name} idx={stream.Index} lang={lang}", LogLevel.Info);
                }
            }
        }

        return subtitles;
    }

    /// <summary>
    /// Whether a media stream is a subtitle track Jellyfin can actually deliver as SRT.
    /// </summary>
    /// <remarks>
    /// Image-based subtitles (PGS/VobSub) and formats Jellyfin's own conversion pipeline
    /// refuses to convert (e.g. ASS/SSA, per MediaStream.SupportsSubtitleConversionTo)
    /// would otherwise be advertised here and then 4xx/produce garbled output when Stremio
    /// requests the Stream.srt URL, since Jellyfin can't actually perform that conversion.
    /// </remarks>
    private static bool IsConvertibleSubtitle(MediaStream stream)
    {
        if (stream.Type != MediaStreamType.Subtitle)
        {
            return false;
        }

        return stream.SupportsSubtitleConversionTo(TargetSubtitleCodec);
    }

    private static string BuildSubtitleUrl(string baseUrl, Guid itemId, string? sourceId, int streamIndex, string authToken)
    {
        return $"{baseUrl}/Videos/{itemId}/{sourceId}/Subtitles/{streamIndex}/0/Stream.srt?api_key={Uri.EscapeDataString(authToken)}";
    }
}
