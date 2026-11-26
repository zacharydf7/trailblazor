using System.Text.Json.Serialization;

namespace Trailblazor;

/// <summary>How a <see cref="GridFilter"/> joins the filters before it on the same column.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterJoin>))]
public enum FilterJoin
{
    /// <summary>The row must meet this filter as well as those before it.</summary>
    And,

    /// <summary>The row may meet this filter instead of those before it.</summary>
    Or,
}
