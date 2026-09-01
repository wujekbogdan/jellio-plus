namespace Jellyfin.Plugin.Jellio.Streams;

public sealed record AudioTrackChoice
{
    /// <summary>
    /// Jellyfin's <c>MediaStream.Index</c> of the audio track.
    /// The index is 0-based and enumerates every stream of the media source, not only the audio ones.
    /// </summary>
    public required int StreamIndex { get; init; }

    /// <summary>
    /// Jellyfin's <c>DisplayTitle</c> for the track, for example "English - Dolby Digital+ - 5.1 - Default".
    /// </summary>
    public required string Label { get; init; }
}
