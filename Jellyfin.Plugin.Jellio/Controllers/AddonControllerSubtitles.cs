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

    // Series subtitle requests come to us keyed by the *episode's* IMDb id (Stremio
    // resolves "tt<id>:<season>:<episode>" through Cinemeta before calling us), not the
    // series' own id - unlike the movie route above, so querying IncludeItemTypes=Series
    // here (as the initial PR did) always returned zero items for series. Match Episode
    // instead, same as GetStreamImdbTv does for the equivalent stream route.
    [HttpGet("subtitles/series/tt{imdbId}:{seasonNum:int}:{episodeNum:int}.json")]
    [HttpGet("subtitles/series/tt{imdbId}:{seasonNum:int}:{episodeNum:int}/{extra}.json")]
    public IActionResult GetSubtitlesImdbSeries([ConfigFromBase64Json] ConfigModel config, string imdbId, int seasonNum, int episodeNum, string? extra = null)
    {
        return GetSubtitlesByImdbId(config, imdbId, BaseItemKind.Episode, extra, seasonNum, episodeNum);
    }

    private IActionResult GetSubtitlesByImdbId(ConfigModel config, string imdbId, BaseItemKind itemKind, string? extra, int? seasonNum = null, int? episodeNum = null)
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

        if (itemKind == BaseItemKind.Episode && seasonNum.HasValue && episodeNum.HasValue)
        {
            items = items.Where(i => i.ParentIndexNumber == seasonNum && i.IndexNumber == episodeNum).ToList();
        }

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
