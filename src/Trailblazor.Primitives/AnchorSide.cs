using System.Text.Json.Serialization;

namespace Trailblazor.Primitives;

[JsonConverter(typeof(JsonStringEnumConverter<AnchorSide>))]
internal enum AnchorSide
{
    Top,
    Bottom,
    Start,
    End,
}
