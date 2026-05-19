using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Jellio.Models;

public class BehaviorHintsDto
{
    [JsonPropertyName("filename")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Filename { get; set; }

    [JsonPropertyName("videoHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VideoHash { get; set; }

    [JsonPropertyName("videoSize")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? VideoSize { get; set; }

    [JsonPropertyName("notWebReady")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool NotWebReady { get; set; }
}