using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbCalendar</c>, found by its label or a locator.</summary>
public sealed class TbCalendarLocator(ILocator scope, ILocator calendar) : TbFieldLocator
{
    private protected override ILocator Control => Root;

    // An id goes on the calendar's grid, or on its date box on a static page, so the calendar is the nearest one up.
    private ILocator Root => calendar
        .Locator("xpath=ancestor-or-self::*[contains(concat(' ', normalize-space(@class), ' '), ' tb-calendar ')][1]")
        .And(scope.Locator(".tb-calendar"));

    private ILocator Grid => Root.Locator("[role=grid]");

    /// <summary>Picks this date as a user would, up to the years, over to its decade, then down through its year and
    /// month to the day, or with <c>Pick</c> to the month or the year, and waits until the calendar has taken it and
    /// the app's <c>ValueChanged</c> has run. A statically rendered calendar gets it in the browser's own date box. A
    /// read-only or disabled calendar throws, and so does a date it doesn't let the user choose.</summary>
    public async Task ChooseAsync(DateOnly date)
    {
        var input = Root.Locator("input[data-static]");
        if (await input.CountAsync() > 0)
        {
            await input.FillAsync(Iso(date));
            return;
        }

        // A prerendered calendar is replaced once the page is interactive, so the calendar's script is waited for.
        await Assertions.Expect(Grid).ToHaveAttributeAsync("data-ready", "");
        if (await Grid.GetAttributeAsync("aria-readonly") == "true"
            || await Grid.GetAttributeAsync("aria-disabled") == "true")
        {
            throw new InvalidOperationException("This calendar is read-only or disabled, so it can't take a date.");
        }

        foreach (var (from, to) in new[] { ("days", "months"), ("months", "years") })
        {
            if (await Grid.GetAttributeAsync("data-view") == from)
            {
                await Root.Locator("[data-part=title]").ClickAsync();
                await Assertions.Expect(Grid).ToHaveAttributeAsync("data-view", to);
            }
        }

        var year = Grid.Locator($"[data-date='{date.Year:0000}-01-01']");
        while (await year.CountAsync() == 0)
        {
            var first = Grid.Locator("[data-date]").First;
            var shown = await first.GetAttributeAsync("data-date");
            var earlier = int.Parse(shown![..4], CultureInfo.InvariantCulture) > date.Year;
            await Root.Locator(earlier ? "[data-part=previous]" : "[data-part=next]").ClickAsync();
            await Assertions.Expect(first).Not.ToHaveAttributeAsync("data-date", shown);
        }

        // A calendar with Pick sets the value at the year or the month, and shows nothing deeper.
        var pick = await Grid.GetAttributeAsync("data-pick");
        var offeredYear = await OfferedAsync(year, date);
        if (pick == "year")
        {
            await PickAsync(offeredYear, $"{date.Year:0000}-01-01");
            return;
        }

        await offeredYear.ClickAsync();
        await Assertions.Expect(Grid).ToHaveAttributeAsync("data-view", "months");
        var firstDay = $"{date.Year:0000}-{date.Month:00}-01";
        var month = await OfferedAsync(Grid.Locator($"[data-date='{firstDay}']"), date);
        if (pick == "month")
        {
            await PickAsync(month, firstDay);
            return;
        }

        await month.ClickAsync();
        await Assertions.Expect(Grid).ToHaveAttributeAsync("data-view", "days");
        await PickAsync(await OfferedAsync(Grid.Locator($"[data-date='{Iso(date)}']"), date), Iso(date));
    }

    // Waits until the app has taken the pick and the calendar marks the cell chosen.
    private async Task PickAsync(ILocator cell, string iso)
    {
        await Commit.WaitAsync(Root, async () =>
        {
            await cell.ClickAsync();
            return true;
        });
        await Assertions.Expect(Grid.Locator($"td:has([data-date='{iso}'])"))
            .ToHaveAttributeAsync("aria-selected", "true");
    }

    // The calendar ignores a click on a year, month or day it doesn't offer, so the app would have nothing to answer.
    private async Task<ILocator> OfferedAsync(ILocator cell, DateOnly date) =>
        await cell.GetAttributeAsync("aria-disabled") == "true"
            ? throw new InvalidOperationException($"This calendar doesn't let {Iso(date)} be chosen.")
            : cell;

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
