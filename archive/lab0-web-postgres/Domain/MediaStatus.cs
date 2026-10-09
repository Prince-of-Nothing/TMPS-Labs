using System.Text.Json.Serialization;

namespace Lab0.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MediaStatus
{
    Planned,
    Consumed
}
