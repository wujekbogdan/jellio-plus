using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Jellio.Models;

public class StreamDto
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("behaviorHints")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BehaviorHintsDto? BehaviorHints { get; set; }
}
