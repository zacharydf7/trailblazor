using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Trailblazor;

// How the library shows a moment: in which zone, and how it writes the zone.
internal static class ShownTime
{
    // A component's own zone, then one cascaded around it, then the app's, then the clock's local zone.
    public static TimeZoneInfo Zone(TimeZoneInfo? own, TimeZoneInfo? cascaded, IServiceProvider services) =>
        own ?? cascaded ?? services.GetService<IOptions<TrailblazorOptions>>()?.Value.Times.TimeZone
            ?? (services.GetService<TimeProvider>() ?? TimeProvider.System).LocalTimeZone;

    // HTML allows at most three digits of a second, where .NET's round-trip "o" format writes seven.
    public static string Iso(DateTimeOffset moment) =>
        moment.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);

    // The app's abbreviation for the zone at that moment, or else its UTC offset, such as UTC−5 or UTC+5:30.
    public static string ZoneName(DateTimeOffset shown, TimeZoneInfo zone, IServiceProvider services, string utc)
    {
        var abbreviations = services.GetService<IOptions<TrailblazorOptions>>()?.Value.Times.Abbreviations;
        if (abbreviations is not null && AbbreviationOf(zone, abbreviations) is { } abbreviation)
        {
            return zone.IsDaylightSavingTime(shown) ? abbreviation.Daylight : abbreviation.Standard;
        }

        var offset = shown.Offset.Duration();
        var hours = offset == TimeSpan.Zero ? ""
            : (shown.Offset < TimeSpan.Zero ? "−" : "+") + offset.Hours.ToString(CultureInfo.CurrentCulture)
                + (offset.Minutes == 0 ? "" : ":" + offset.Minutes.ToString("00", CultureInfo.CurrentCulture));
        return string.Format(CultureInfo.CurrentCulture, utc, hours);
    }

    private static TimeZoneAbbreviation? AbbreviationOf(TimeZoneInfo zone,
        IDictionary<string, TimeZoneAbbreviation> abbreviations)
    {
        if (abbreviations.TryGetValue(zone.Id, out var found))
        {
            return found;
        }

        var other = TimeZoneInfo.TryConvertIanaIdToWindowsId(zone.Id, out var windows) ? windows
            : TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var iana) ? iana : null;
        return other is not null && abbreviations.TryGetValue(other, out found) ? found : null;
    }
}
