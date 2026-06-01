using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellio.Helpers;
using Jellyfin.Plugin.Jellio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Jellyfin.Plugin.Jellio.Controllers;

[ApiController]
[ConfigAuthorize]
[Route("jelliopp/{config}/jellyseerr")]
public class RequestController : ControllerBase
{
    // Simple in-memory cache to prevent duplicate requests (userId:imdbId:type -> timestamp)
    private static readonly ConcurrentDictionary<string, DateTime> _requestCache = new();
    private static readonly ConcurrentDictionary<string, object> _requestLocks = new();
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);

    private static bool TryMarkAsProcessing(Guid userId, string identifier, string type)
    {
        var cacheKey = $"{userId}:{identifier}:{type}";
        var lockObj = _requestLocks.GetOrAdd(cacheKey, _ => new object());

        lock (lockObj)
        {
            if (_requestCache.TryGetValue(cacheKey, out var timestamp))
            {
                if (DateTime.UtcNow - timestamp < _cacheDuration)
                {
                    var msg = $"[Jellyseerr] Skipping duplicate request (cached {(DateTime.UtcNow - timestamp).TotalSeconds:F1}s ago)";
                    Console.WriteLine(msg);
                    LogBuffer.AddLog(msg, LogLevel.Info);
                    return false; // Already requested
                }
            }

            // Mark as being processed NOW
            _requestCache[cacheKey] = DateTime.UtcNow;
            return true; // OK to process
        }
    }

    private static HttpClient CreateHttpClient(string baseUrl, string? apiKey)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
        };
        client.Timeout = TimeSpan.FromSeconds(10);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            // API keys from the config are stored in plain text, use directly
            var msg = $"[Jellyseerr] Using API key: {apiKey.Substring(0, Math.Min(8, apiKey.Length))}... (length: {apiKey.Length})";
            Console.WriteLine(msg);
            LogBuffer.AddLog(msg, LogLevel.Info);
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }
        else
        {
            var msg = "[Jellyseerr] WARNING: No API key provided!";
            Console.WriteLine(msg);
            LogBuffer.AddLog(msg, LogLevel.Warning);
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
        [FromQuery] int? episode
    )
    {
        try
        {
            var requestMsg = $"[Jellyseerr] Request received: type={type}, tmdbId={tmdbId}, imdbId={imdbId}, title={title}";
            Console.WriteLine(requestMsg);
            LogBuffer.AddLog(requestMsg, LogLevel.Info);

            if (config is null)
            {
                var errorMsg = "[Jellyseerr] ERROR: Config is null";
                Console.WriteLine(errorMsg);
                LogBuffer.AddLog(errorMsg, LogLevel.Error);
                return BadRequest("Invalid or missing configuration.");
            }

            // Get userId from context (set by ConfigAuthorize filter)
            var userId = (Guid?)HttpContext.Items["JellioUserId"];
            if (userId == null)
            {
                var errorMsg = "[Jellyseerr] ERROR: No user ID in context";
                Console.WriteLine(errorMsg);
                LogBuffer.AddLog(errorMsg, LogLevel.Error);
                return Unauthorized();
            }

            // Check for duplicate request (with lock to prevent race condition)
            var identifier = imdbId ?? tmdbId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? title ?? "unknown";
            if (!TryMarkAsProcessing(userId.Value, identifier, type))
            {
                var dupHtml = """
                    <!DOCTYPE html>
                    <html lang="en">
                    <head>
                      <meta charset="utf-8">
                      <meta name="viewport" content="width=device-width, initial-scale=1">
                      <title>Jellyseerr – Already Requested</title>
                      <style>
                        body { font-family: sans-serif; background: #1a1a2e; color: #e0e0e0;
                               display: flex; align-items: center; justify-content: center;
                               min-height: 100vh; margin: 0; }
                        .card { background: #16213e; border-radius: 12px; padding: 40px 50px;
                                max-width: 500px; text-align: center; box-shadow: 0 4px 30px rgba(0,0,0,.5); }
                        .icon { font-size: 3rem; margin-bottom: 16px; }
                        h1 { color: #facc15; margin: 0 0 12px; font-size: 1.6rem; }
                        p { color: #9ca3af; margin: 0; line-height: 1.5; }
                      </style>
                    </head>
                    <body>
                      <div class="card">
                        <div class="icon">⏳</div>
                        <h1>Already requested</h1>
                        <p>This request was already sent recently. Duplicate prevented.</p>
                      </div>
                    </body>
                    </html>
                    """;
                return Content(dupHtml, "text/html");
            }

            var configMsg = $"[Jellyseerr] Config loaded: Enabled={config.JellyseerrEnabled}, Url={config.JellyseerrUrl}, HasApiKey={!string.IsNullOrWhiteSpace(config.JellyseerrApiKey)}";
            Console.WriteLine(configMsg);
            LogBuffer.AddLog(configMsg, LogLevel.Info);

            if (!config.JellyseerrEnabled || string.IsNullOrWhiteSpace(config.JellyseerrUrl))
            {
                var errorMsg = "[Jellyseerr] ERROR: Jellyseerr not configured or disabled";
                Console.WriteLine(errorMsg);
                LogBuffer.AddLog(errorMsg, LogLevel.Error);
                return BadRequest("Jellyseerr is not configured.");
            }

            int? maybeTmdbId = tmdbId;

            using var client = CreateHttpClient(config.JellyseerrUrl!, config.JellyseerrApiKey);

            // Resolve TMDB ID via Jellyseerr search if not provided
            if (maybeTmdbId is null)
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    Console.WriteLine("[Jellyseerr] ERROR: No tmdbId or title provided");
                    return Problem("Either tmdbId or title parameter is required.", statusCode: 400);
                }

                Console.WriteLine($"[Jellyseerr] Searching Jellyseerr for title: {title}");
                var searchUri = $"api/v1/search?query={Uri.EscapeDataString(title!)}";
                using var resp = await client.GetAsync(searchUri);
                Console.WriteLine($"[Jellyseerr] Search response status: {resp.StatusCode}");

                if (resp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStreamAsync());
                    if (doc.RootElement.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"[Jellyseerr] Found {results.GetArrayLength()} search results");
                        foreach (var el in results.EnumerateArray())
                        {
                            var mediaType = el.TryGetProperty("mediaType", out var mt) ? mt.GetString() : null;
                            if (!string.IsNullOrEmpty(mediaType) && string.Equals(mediaType, type, StringComparison.OrdinalIgnoreCase))
                            {
                                if (el.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var idVal))
                                {
                                    maybeTmdbId = idVal;
                                    Console.WriteLine($"[Jellyseerr] Matched TMDB ID: {idVal}");
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Jellyseerr] Search failed: {errorContent}");
                }
            }

            if (maybeTmdbId is null)
            {
                Console.WriteLine("[Jellyseerr] ERROR: Could not resolve TMDB ID");
                return Problem("Unable to resolve TMDB id for request.", statusCode: 502);
            }

            int id = maybeTmdbId.Value;
            Console.WriteLine($"[Jellyseerr] Using TMDB ID: {id}");

            bool isTV = string.Equals(type, "tv", StringComparison.OrdinalIgnoreCase);

            // Build request body - only include seasons for TV shows
            object body;
            if (isTV)
            {
                int[]? seasons = null;
                if (season.HasValue)
                {
                    seasons = new[] { season.Value };
                    Console.WriteLine($"[Jellyseerr] Requesting TV season: {season.Value}");
                }

                body = new
                {
                    mediaType = "tv",
                    mediaId = id,
                    seasons
                };
            }
            else
            {
                // For movies, don't include seasons field at all
                body = new
                {
                    mediaType = "movie",
                    mediaId = id
                };
            }

            Console.WriteLine($"[Jellyseerr] Sending request to Jellyseerr: {config.JellyseerrUrl}/api/v1/request");
            using var createResp = await client.PostAsJsonAsync("api/v1/request", body);
            Console.WriteLine($"[Jellyseerr] Request response status: {createResp.StatusCode}");

            if (createResp.IsSuccessStatusCode)
            {
                Console.WriteLine("[Jellyseerr] ✓ Request successful!");

                var displayTitle = !string.IsNullOrWhiteSpace(title) ? title : imdbId ?? "content";
                var successHtml = $"""
                    <!DOCTYPE html>
                    <html lang="en">
                    <head>
                      <meta charset="utf-8">
                      <meta name="viewport" content="width=device-width, initial-scale=1">
                      <title>Jellyseerr – Request Sent</title>
                      <style>
                        body {{ font-family: sans-serif; background: #1a1a2e; color: #e0e0e0;
                               display: flex; align-items: center; justify-content: center;
                               min-height: 100vh; margin: 0; }}
                        .card {{ background: #16213e; border-radius: 12px; padding: 40px 50px;
                                 max-width: 500px; text-align: center; box-shadow: 0 4px 30px rgba(0,0,0,.5); }}
                        .icon {{ font-size: 3rem; margin-bottom: 16px; }}
                        h1 {{ color: #4ade80; margin: 0 0 12px; font-size: 1.6rem; }}
                        p {{ color: #9ca3af; margin: 0; line-height: 1.5; }}
                        .title {{ color: #e0e0e0; font-weight: bold; }}
                      </style>
                    </head>
                    <body>
                      <div class="card">
                        <div class="icon">✅</div>
                        <h1>Request sent!</h1>
                        <p>Your request for <span class="title">{System.Web.HttpUtility.HtmlEncode(displayTitle)}</span>
                           has been submitted to Jellyseerr.<br>You can close this tab.</p>
                      </div>
                    </body>
                    </html>
                    """;
                return Content(successHtml, "text/html");
            }

            var failContent = await createResp.Content.ReadAsStringAsync();
            Console.WriteLine($"[Jellyseerr] ERROR: Request failed with: {failContent}");

            var errorHtml = $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>Jellyseerr – Request Failed</title>
                  <style>
                    body {{ font-family: sans-serif; background: #1a1a2e; color: #e0e0e0;
                             display: flex; align-items: center; justify-content: center;
                             min-height: 100vh; margin: 0; }}
                    .card {{ background: #16213e; border-radius: 12px; padding: 40px 50px;
                             max-width: 500px; text-align: center; box-shadow: 0 4px 30px rgba(0,0,0,.5); }}
                    .icon {{ font-size: 3rem; margin-bottom: 16px; }}
                    h1 {{ color: #f87171; margin: 0 0 12px; font-size: 1.6rem; }}
                    p {{ color: #9ca3af; margin: 0; line-height: 1.5; }}
                    code {{ background: #0f3460; padding: 2px 6px; border-radius: 4px; font-size: .85rem; }}
                  </style>
                </head>
                <body>
                  <div class="card">
                    <div class="icon">❌</div>
                    <h1>Request failed</h1>
                    <p>Jellyseerr returned status <code>{(int)createResp.StatusCode}</code>.<br>
                       Check your Jellyseerr configuration and API key.</p>
                  </div>
                </body>
                </html>
                """;
            return Content(errorHtml, "text/html", System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            var errorMsg = $"[Jellyseerr] EXCEPTION: {ex.Message}";
            var stackMsg = $"[Jellyseerr] Stack trace: {ex.StackTrace}";
            Console.WriteLine(errorMsg);
            Console.WriteLine(stackMsg);
            LogBuffer.AddLog(errorMsg, LogLevel.Error);
            LogBuffer.AddLog(stackMsg, LogLevel.Error);

            var exHtml = $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>Jellyseerr – Error</title>
                  <style>
                    body {{ font-family: sans-serif; background: #1a1a2e; color: #e0e0e0;
                             display: flex; align-items: center; justify-content: center;
                             min-height: 100vh; margin: 0; }}
                    .card {{ background: #16213e; border-radius: 12px; padding: 40px 50px;
                             max-width: 500px; text-align: center; box-shadow: 0 4px 30px rgba(0,0,0,.5); }}
                    .icon {{ font-size: 3rem; margin-bottom: 16px; }}
                    h1 {{ color: #f87171; margin: 0 0 12px; font-size: 1.6rem; }}
                    p {{ color: #9ca3af; margin: 0; line-height: 1.5; }}
                    code {{ background: #0f3460; padding: 2px 6px; border-radius: 4px; font-size: .85rem; word-break: break-all; }}
                  </style>
                </head>
                <body>
                  <div class="card">
                    <div class="icon">⚠️</div>
                    <h1>Unexpected error</h1>
                    <p><code>{System.Web.HttpUtility.HtmlEncode(ex.Message)}</code><br><br>
                       Check the Jellyfin plugin logs for details.</p>
                  </div>
                </body>
                </html>
                """;
            return Content(exHtml, "text/html", System.Text.Encoding.UTF8);
        }
    }
}
