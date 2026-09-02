using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellio.Helpers;
using Jellyfin.Plugin.Jellio.Models;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellio.Controllers;

public partial class AddonController
{
    [HttpGet("subtitles/{stremioType}/jelliopp:{mediaId:guid}.json")]
    [HttpGet("subtitles/{stremioType}/jelliopp:{mediaId:guid}/{extra}.json")]
    public IActionResult GetSubtitles([ConfigFromBase64Json] ConfigModel config, StremioType stremioType, Guid mediaId, string? extra = null)
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;
        LogBuffer.AddLog($"[Subtitles] req guid={mediaId} extra={extra}", LogLevel.Info);
        var item = _libraryManager.GetItemById<BaseItem>(mediaId, userId);
        if (item == null)
        {
            return Ok(new { subtitles = Array.Empty<object>() });
        }

        return BuildSubtitlesResult(userId, [item], config.AuthToken, config.PublicBaseUrl);
    }

    [HttpGet("subtitles/movie/tt{imdbId}.json")]
    [HttpGet("subtitles/movie/tt{imdbId}/{extra}.json")]
    public IActionResult GetSubtitlesImdbMovie([ConfigFromBase64Json] ConfigModel config, string imdbId, string? extra = null)
    {
        return GetSubtitlesByImdbId(config, imdbId, BaseItemKind.Movie, extra);
    }

    // Per Stremio's own protocol/convention (and confirmed live against Stremio's
    // first-party OpenSubtitles v3 addon, e.g. subtitles/series/tt0388629:1:763/*.json
    // for "One Piece", where tt0388629 is the *series'* IMDb id), the id in
    // "tt<id>:<season>:<episode>" is the series' own IMDb id, not the episode's -
    // matching how the stream route (GetStreamImdbTv) already resolves series first,
    // then the specific episode by ancestor + season/episode index.
    [HttpGet("subtitles/series/tt{imdbId}:{seasonNum:int}:{episodeNum:int}.json")]
    [HttpGet("subtitles/series/tt{imdbId}:{seasonNum:int}:{episodeNum:int}/{extra}.json")]
    public IActionResult GetSubtitlesImdbSeries([ConfigFromBase64Json] ConfigModel config, string imdbId, int seasonNum, int episodeNum, string? extra = null)
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;
        LogBuffer.AddLog($"[Subtitles] req imdb=tt{imdbId} kind=Series season={seasonNum} episode={episodeNum} extra={extra}", LogLevel.Info);
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var seriesQuery = new InternalItemsQuery(user)
        {
            HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = $"tt{imdbId}" },
            IncludeItemTypes = [BaseItemKind.Series],
        };
        var seriesItems = _libraryManager.GetItemList(seriesQuery);

        if (seriesItems.Count == 0)
        {
            return BuildSubtitlesResult(userId, [], config.AuthToken, config.PublicBaseUrl);
        }

        var seriesIds = seriesItems.Select(s => s.Id).ToArray();
        var episodeQuery = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Episode],
            AncestorIds = seriesIds,
            ParentIndexNumber = seasonNum,
            IndexNumber = episodeNum,
        };
        var episodeItems = _libraryManager.GetItemList(episodeQuery);

        return BuildSubtitlesResult(userId, episodeItems, config.AuthToken, config.PublicBaseUrl);
    }

    private IActionResult GetSubtitlesByImdbId(ConfigModel config, string imdbId, BaseItemKind itemKind, string? extra)
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;
        LogBuffer.AddLog($"[Subtitles] req imdb=tt{imdbId} kind={itemKind} extra={extra}", LogLevel.Info);
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var query = new InternalItemsQuery(user)
        {
            HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = $"tt{imdbId}" },
            IncludeItemTypes = [itemKind],
        };

        IReadOnlyList<BaseItem> items = _libraryManager.GetItemList(query);

        return BuildSubtitlesResult(userId, items, config.AuthToken, config.PublicBaseUrl);
    }

    private OkObjectResult BuildSubtitlesResult(Guid userId, IReadOnlyList<BaseItem> items, string authToken, string? publicBaseUrl)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return Ok(new { subtitles = Array.Empty<object>() });
        }

        var baseUrl = GetBaseUrl(publicBaseUrl);
        var dtos = _dtoService.GetBaseItemDtos(items, new DtoOptions(true), user);
        var subtitles = SubtitleHelper.BuildSubtitleDtos(dtos, baseUrl, authToken);

        return Ok(new { subtitles });
    }
}
