using System.Text.Json.Serialization;

namespace Trailblazor.Primitives;

[JsonConverter(typeof(JsonStringEnumConverter<AnchorAlign>))]
internal enum AnchorAlign
{
    Start,
    Center,
    End,
}
