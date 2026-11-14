using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbCalendar</c>, found by its label.</summary>
public sealed class TbCalendarLocator(ILocator scope, string label) : TbFieldLocator
{
    private protected override ILocator Control => Root;

    private ILocator Root => scope.GetByRole(AriaRole.Group, new() { NameRegex = FieldLabel.Matching(label) })
        .And(scope.Locator(".tb-calendar"));

    private ILocator Grid => Root.Locator("[role=grid]");

    /// <summary>Picks this date as a user would, up to the years, over to its decade, then down through its year and
    /// month to the day, and waits until the calendar has taken it. A statically rendered calendar gets it in the
    /// browser's own date box.</summary>
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

        await year.ClickAsync();
        await Assertions.Expect(Grid).ToHaveAttributeAsync("data-view", "months");
        await Grid.Locator($"[data-date='{date.Year:0000}-{date.Month:00}-01']").ClickAsync();
        await Assertions.Expect(Grid).ToHaveAttributeAsync("data-view", "days");
        await Grid.Locator($"[data-date='{Iso(date)}']").ClickAsync();
        await Assertions.Expect(Grid.Locator($"td:has([data-date='{Iso(date)}'])"))
            .ToHaveAttributeAsync("aria-selected", "true");
    }

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
