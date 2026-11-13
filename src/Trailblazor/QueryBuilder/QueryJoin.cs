using System.Text.Json.Serialization;

namespace Trailblazor;

/// <summary>How a <see cref="QueryGroup"/> joins its conditions and inner groups.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<QueryJoin>))]
public enum QueryJoin
{
    /// <summary>An item matches when it meets all of them.</summary>
    And,

    /// <summary>An item matches when it meets any of them.</summary>
    Or,
}
