namespace Trailblazor;

/// <summary>How a <see cref="TbTimestamp"/> writes its moment, in the current culture's short patterns.</summary>
public enum TimestampFormat
{
    /// <summary>The date alone, such as 11/25/2026.</summary>
    Date,

    /// <summary>The time alone, such as 3:45 PM.</summary>
    Time,

    /// <summary>The date and the time, such as 11/25/2026 3:45 PM.</summary>
    DateTime,

    /// <summary>How far the moment is from now, such as "in 3 days", "2 hours ago" or "just now", with the date and
    /// time in a tooltip that is also its description. It keeps up as time passes. Under 45 seconds is "just now";
    /// then minutes up to 45, hours up to 22, days up to 26, months up to 320 days, and years, each rounded.</summary>
    Relative,
}
