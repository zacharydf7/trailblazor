using System.Globalization;

namespace Trailblazor.Testing.Playwright;

// The date components read and show dates in their culture's Gregorian calendar, so typed text has to use it too.
internal static class GregorianCulture
{
    public static CultureInfo Named(string? name)
    {
        var culture = CultureInfo.GetCultureInfo(name ?? "");
        if (culture.DateTimeFormat.Calendar is GregorianCalendar
            || culture.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault() is not { } gregorian)
        {
            return culture;
        }

        var copy = (CultureInfo)culture.Clone();
        copy.DateTimeFormat.Calendar = gregorian;
        return copy;
    }
}
