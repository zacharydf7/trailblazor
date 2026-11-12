using System.Text.Json.Serialization;

namespace Trailblazor;

/// <summary>Where a <see cref="TreeMove{TItem}"/> puts the moved items, beside or inside its target.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TreeMovePosition>))]
public enum TreeMovePosition
{
    /// <summary>Just before the target, among its siblings.</summary>
    Before,

    /// <summary>Just after the target, among its siblings.</summary>
    After,

    /// <summary>Inside the target, as its last children.</summary>
    Inside,
}
