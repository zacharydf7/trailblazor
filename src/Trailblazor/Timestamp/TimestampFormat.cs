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
}
