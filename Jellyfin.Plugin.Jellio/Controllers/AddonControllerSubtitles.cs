using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellio.Helpers;
using Jellyfin.Plugin.Jellio.Models;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
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

        return BuildSubtitles(userId, [item], config.AuthToken, config.PublicBaseUrl);
    }

    [HttpGet("subtitles/movie/tt{imdbId}.json")]
    [HttpGet("subtitles/movie/tt{imdbId}/{extra}.json")]
    public IActionResult GetSubtitlesImdbMovie([ConfigFromBase64Json] ConfigModel config, string imdbId, string? extra = null)
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;
        LogBuffer.AddLog($"[Subtitles] req imdb=tt{imdbId} extra={extra}", LogLevel.Info);
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var query = new InternalItemsQuery(user)
        {
            HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = $"tt{imdbId}" },
            IncludeItemTypes = [BaseItemKind.Movie],
        };
        return BuildSubtitles(userId, _libraryManager.GetItemList(query), config.AuthToken, config.PublicBaseUrl);
    }

    private OkObjectResult BuildSubtitles(Guid userId, IReadOnlyList<BaseItem> items, string authToken, string? publicBaseUrl)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return Ok(new { subtitles = Array.Empty<object>() });
        }

        var baseUrl = GetBaseUrl(publicBaseUrl);
        var dtos = _dtoService.GetBaseItemDtos(items, new DtoOptions(true), user);
        var subtitles = new List<SubtitleDto>();

        foreach (var dto in dtos)
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

                foreach (var ms in source.MediaStreams)
                {
                    if (ms.Type != MediaStreamType.Subtitle)
                    {
                        continue;
                    }

                    var lang = string.IsNullOrEmpty(ms.Language) ? "und" : ms.Language;
                    var url = $"{baseUrl}/Videos/{dto.Id}/{source.Id}/Subtitles/{ms.Index}/0/Stream.srt?api_key={Uri.EscapeDataString(authToken)}";
                    subtitles.Add(new SubtitleDto { Id = $"jelliopp-{dto.Id}-{ms.Index}", Url = url, Lang = lang });
                    LogBuffer.AddLog($"[Subtitles] {dto.Name} idx={ms.Index} lang={lang}", LogLevel.Info);
                }
            }
        }

        return Ok(new { subtitles });
    }
}
