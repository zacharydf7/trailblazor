namespace Trailblazor;

/// <summary>Which time zone the library shows dates and times in, set in <c>AddTrailblazor(options => ...)</c>.
/// </summary>
public sealed class TimeSettings
{
    /// <summary>The zone timestamps, chat and timeline times and data grid <see cref="DateTimeOffset"/> columns show
    /// in, for an app that shows one zone to everyone; the date pickers show a value as it is written. A
    /// <see cref="TimeZoneInfo"/> cascaded around the components, such as each user's own, wins over it, and a
    /// component's own <c>TimeZone</c> wins over both. Left null, they show in the <see cref="TimeProvider"/>'s local
    /// zone, which on Blazor Server is the server's.</summary>
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>The abbreviations a <see cref="TbTimestamp"/> with <see cref="TbTimestamp.ShowZone"/> shows in place of
    /// the UTC offset, by the zone's id, such as <c>["America/New_York"] = new("EST", "EDT")</c>, since .NET has none
    /// of its own. A zone's IANA id and its Windows id both find it.</summary>
    public IDictionary<string, TimeZoneAbbreviation> Abbreviations { get; } =
        new Dictionary<string, TimeZoneAbbreviation>();
}

/// <summary>What a time zone is called in its standard and its daylight saving time, such as EST and EDT.</summary>
/// <param name="Standard">The abbreviation in standard time, such as "EST".</param>
/// <param name="Daylight">The abbreviation in daylight saving time, such as "EDT".</param>
public sealed record TimeZoneAbbreviation(string Standard, string Daylight);
