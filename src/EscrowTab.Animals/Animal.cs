using System.Text.Json;
using System.Text.Json.Serialization;

namespace EscrowTab.Animals;

/// <summary>
///     Model type representing the 'Animal'.
///     For simplicity, this class is being used as both an EF model and for JSON serialization.
///     More complex API implementations would likely require separate model classes.
/// </summary>
[JsonConverter(typeof(AnimalConverter))] // So we can have the Id as the property name in JSON.
internal class Animal
{
    public long Id { get; set; }

    public string Label { get; set; } = "";

    // Parent property is needed for EF, but not for serialization.
    // Will cause an infinite loop if [JsonIgnore] is removed.
    [JsonIgnore] public Animal? Parent { get; set; }

    public List<Animal> Children { get; } = new();
}

internal class AnimalConverter : JsonConverter<Animal>
{
    public override Animal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Only used for serialization, so no need to deserialize.
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, Animal value, JsonSerializerOptions options)
    {
        var dictionary = new Dictionary<long, object>
        {
            {
                value.Id, new
                {
                    label = value.Label,
                    children = value.Children
                }
            }
        };
        writer.WriteRawValue(JsonSerializer.Serialize(dictionary));
    }
}