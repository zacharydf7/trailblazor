using System.Text.Json.Serialization;

namespace Trailblazor.Primitives;

[JsonConverter(typeof(JsonStringEnumConverter<DismissReason>))]
internal enum DismissReason
{
    EscapeKey,
    PointerDownOutside,
    FocusOutside,
}
