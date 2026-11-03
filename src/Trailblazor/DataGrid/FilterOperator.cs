namespace Trailblazor;

/// <summary>How a <see cref="GridFilter"/> compares a column's values with its own.</summary>
public enum FilterOperator
{
    /// <summary>The column's text contains the filter's, ignoring case.</summary>
    Contains,

    /// <summary>The column's text doesn't contain the filter's, ignoring case.</summary>
    DoesNotContain,

    /// <summary>The column's text starts with the filter's, ignoring case.</summary>
    StartsWith,

    /// <summary>The column's text ends with the filter's, ignoring case.</summary>
    EndsWith,

    /// <summary>The value equals the filter's.</summary>
    EqualTo,

    /// <summary>The value differs from the filter's, or there is no value.</summary>
    NotEqualTo,

    /// <summary>The value is less than the filter's.</summary>
    LessThan,

    /// <summary>The value is less than or equal to the filter's.</summary>
    LessThanOrEqualTo,

    /// <summary>The value is greater than the filter's.</summary>
    GreaterThan,

    /// <summary>The value is greater than or equal to the filter's.</summary>
    GreaterThanOrEqualTo,

    /// <summary>There is no value, or it is an empty string. Whitespace isn't empty.</summary>
    IsEmpty,

    /// <summary>There is a value, and it isn't an empty string.</summary>
    IsNotEmpty,

    /// <summary>The value equals one of the values in the filter's list.</summary>
    IsAnyOf,
}
