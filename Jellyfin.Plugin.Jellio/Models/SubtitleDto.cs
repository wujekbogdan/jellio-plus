using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Jellio.Models;

public class SubtitleDto
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("lang")]
    public required string Lang { get; set; }
}
