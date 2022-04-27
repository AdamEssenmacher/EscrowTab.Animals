using System.Text.Json.Serialization;

namespace EscrowTab.Animals;

internal class PostDto
{
    [JsonPropertyName("parent")] public long Parent { get; set; }

    [JsonPropertyName("label")] public string Label { get; set; } = "";
}