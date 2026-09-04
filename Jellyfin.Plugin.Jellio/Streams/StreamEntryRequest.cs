using System;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Plugin.Jellio.Streams;

public sealed record StreamEntryRequest
{
    public required Guid ItemId { get; init; }

    public required MediaSourceInfo MediaSource { get; init; }

    public required bool IsMultiMediaSource { get; init; }

    public required int SourceIndex { get; init; }

    public required string BaseUrl { get; init; }

    public required string AuthToken { get; init; }
}
