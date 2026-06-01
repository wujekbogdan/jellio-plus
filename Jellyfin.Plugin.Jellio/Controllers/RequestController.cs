using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellio.Helpers;
using Jellyfin.Plugin.Jellio.Models;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellio.Controllers;

[ApiController]
[ConfigAuthorize]
[Route("jelliopp/{config}/jellyseerr")]
public class RequestController : ControllerBase
{
    // Tracks "this user has seen this item" - first GET marks it, second GET triggers request
    private static readonly ConcurrentDictionary<string, DateTime> _seenCache = new();
    // Tracks "request already sent" - prevents duplicate sends
    private static readonly ConcurrentDictionary<string, DateTime> _sentCache = new();
    private static readonly object _cacheLock = new();

    // Window in which a second GET counts as a "user click" (not another prefetch)
    private static readonly TimeSpan _clickWindow = TimeSpan.FromSeconds(2);
    // How long to remember "already sent" to prevent spam
    private static readonly TimeSpan _sentCacheDuration = TimeSpan.FromHours(24);

    /// <summary>
    /// Two-step detection:
    /// - 1st GET within prefetch window → mark as "seen", return harmless response
    /// - 2nd GET after click window → real user click → send Jellyseerr request
    /// - subsequent GETs → block (already sent)
    /// </summary>
    private static ClickState ClassifyRequest(Guid userId, string identifier, string type)
    {
        var cacheKey = $"{userId}:{identifier}:{type}";
        var now = DateTime.UtcNow;

        lock (_cacheLock)
        {
            // Cleanup old entries (cheap housekeeping)
            CleanupExpired(now);

            // Already sent recently? Block.
            if (_sentCache.TryGetValue(cacheKey, out var sentAt) &&
                now - sentAt < _sentCacheDuration)
            {
                return ClickState.AlreadySent;
            }

            // Already seen?
            if (_seenCache.TryGetValue(cacheKey, out var seenAt))
            {
                var elapsed = now - seenAt;
                if (elapsed >= _clickWindow)
                {
                    // 2nd GET after click window → real user click
                    _sentCache[cacheKey] = now;
                    _seenCache.TryRemove(cacheKey, out _);
                    return ClickState.UserClick;
                }
                // 2nd GET within click window → likely still part of prefetch burst
                return ClickState.Prefetch;
            }

            // First time seeing this → mark as seen, treat as prefetch
            _seenCache[cacheKey] = now;
            return ClickState.Prefetch;
        }
    }

    private static void CleanupExpired(DateTime now)
    {
        foreach (var kvp in _seenCache)
        {
            if (now - kvp.Value > TimeSpan.FromMinutes(10))
            {
                _seenCache.TryRemove(kvp.Key, out _);
            }
        }
        foreach (var kvp in _sentCache)
        {
            if (now - kvp.Value > _sentCacheDuration)
            {
                _sentCache.TryRemove(kvp.Key, out _);
            }
        }
    }

    private enum ClickState
    {
        Prefetch,      // 1st GET (or burst) → do nothing
        UserClick,     // 2nd GET after delay → send request
        AlreadySent    // Already done within 24h → block
    }

    private static HttpClient CreateHttpClient(string baseUrl, string? apiKey)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }
        return client;
    }

    [HttpGet]
    public async Task<IActionResult> CreateRequest(
        [ConfigFromBase64Json] ConfigModel? config,
        [FromQuery] string type,
        [FromQuery] int? tmdbId,
        [FromQuery] string? imdbId,
        [FromQuery] string? title,
        [FromQuery] int? season,
        [FromQuery] int? episode)
    {
        try
        {
            if (config is null)
            {
                return BadRequest("Invalid or missing configuration.");
            }

            var userId = (Guid?)HttpContext.Items["JellioUserId"];
            if (userId == null)
            {
                return Unauthorized();
            }

            var identifier = imdbId
                ?? tmdbId?.ToString(System.Globalization.CultureInfo.InvariantCulture)
                ?? title
                ?? "unknown";

            var state = ClassifyRequest(userId.Value, identifier, type);

            switch (state)
            {
                case ClickState.Prefetch:
                    LogBuffer.AddLog(
                        $"[Jellyseerr] Prefetch detected for {identifier} ({type}) - waiting for user click",
                        LogLevel.Info);
                    // Return 404 so the player won't try to start anything,
                    // but no request is sent to Jellyseerr.
                    return NotFound();

                case ClickState.AlreadySent:
                    LogBuffer.AddLog(
                        $"[Jellyseerr] Request for {identifier} ({type}) already sent recently - blocking",
                        LogLevel.Info);
                    return NotFound();

                case ClickState.UserClick:
                    LogBuffer.AddLog(
                        $"[Jellyseerr] User click confirmed for {identifier} ({type}) - sending request",
                        LogLevel.Info);
                    break;
            }

            // ===== Real user click - proceed with Jellyseerr request =====

            if (!config.JellyseerrEnabled || string.IsNullOrWhiteSpace(config.JellyseerrUrl))
            {
                LogBuffer.AddLog("[Jellyseerr] ERROR: Jellyseerr not configured", LogLevel.Error);
                return BadRequest("Jellyseerr is not configured.");
            }

            int? maybeTmdbId = tmdbId;
            using var client = CreateHttpClient(config.JellyseerrUrl!, config.JellyseerrApiKey);

            // Resolve TMDB ID via search if not provided
            if (maybeTmdbId is null)
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    return Problem("Either tmdbId or title parameter is required.", statusCode: 400);
                }

                LogBuffer.AddLog($"[Jellyseerr] Searching for: {title}", LogLevel.Info);
                var searchUri = $"api/v1/search?query={Uri.EscapeDataString(title!)}";
                using var resp = await client.GetAsync(searchUri);

                if (resp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStreamAsync());
                    if (doc.RootElement.TryGetProperty("results", out var results) &&
                        results.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in results.EnumerateArray())
                        {
                            var mediaType = el.TryGetProperty("mediaType", out var mt) ? mt.GetString() : null;
                            if (!string.IsNullOrEmpty(mediaType) &&
                                string.Equals(mediaType, type, StringComparison.OrdinalIgnoreCase))
                            {
                                if (el.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var idVal))
                                {
                                    maybeTmdbId = idVal;
                                    LogBuffer.AddLog($"[Jellyseerr] Matched TMDB ID: {idVal}", LogLevel.Info);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (maybeTmdbId is null)
            {
                LogBuffer.AddLog("[Jellyseerr] ERROR: Could not resolve TMDB ID", LogLevel.Error);
                return Problem("Unable to resolve TMDB id for request.", statusCode: 502);
            }

            int id = maybeTmdbId.Value;
            bool isTV = string.Equals(type, "tv", StringComparison.OrdinalIgnoreCase);

            object body;
            if (isTV)
            {
                int[]? seasons = season.HasValue ? new[] { season.Value } : null;
                body = new { mediaType = "tv", mediaId = id, seasons };
            }
            else
            {
                body = new { mediaType = "movie", mediaId = id };
            }

            LogBuffer.AddLog($"[Jellyseerr] Sending request to {config.JellyseerrUrl}/api/v1/request", LogLevel.Info);
            using var createResp = await client.PostAsJsonAsync("api/v1/request", body);

            if (createResp.IsSuccessStatusCode)
            {
                LogBuffer.AddLog("[Jellyseerr] ✓ Request successful!", LogLevel.Info);
                return Content("✓ Content request sent to Jellyseerr successfully!", "text/plain");
            }

            var failContent = await createResp.Content.ReadAsStringAsync();
            LogBuffer.AddLog($"[Jellyseerr] ERROR: Request failed: {failContent}", LogLevel.Error);
            return Problem($"Jellyseerr request failed with status {(int)createResp.StatusCode}.", statusCode: 502);
        }
        catch (Exception ex)
        {
            LogBuffer.AddLog($"[Jellyseerr] EXCEPTION: {ex.Message}", LogLevel.Error);
            LogBuffer.AddLog($"[Jellyseerr] Stack: {ex.StackTrace}", LogLevel.Error);
            return Problem($"Error creating Jellyseerr request: {ex.Message}", statusCode: 500);
        }
    }
}

