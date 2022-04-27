using System.Text.Json.Serialization;

namespace EscrowTab.Animals;

internal class PutDto
{
    [JsonPropertyName("current-id")] public long CurrentId { get; set; }
}