namespace Trailblazor.BrowserTests;

public class GridScrollTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualRows_ShouldScrollToARowClearOfTheHeader_WhenItIsntDrawnYet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var table = page.GetByRole(AriaRole.Table, new() { Name = "Readings", Exact = true });
        await Expect(table.Locator("tbody tr[data-part=row]").First).ToBeVisibleAsync();
        await Expect(table.Locator("tr[data-key='5000']")).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = "Go to reading 5000" }).ClickAsync();

        await Expect(table.Locator("tr[data-key='5000']")).ToBeInViewportAsync();
        var readings = await table.ElementHandleAsync();
        const string Clear = """
            table => {
                const row = table.querySelector("tr[data-key='5000']");
                const box = row?.getBoundingClientRect();
                return row?.contains(document.elementFromPoint(box.left + 4, box.top + 4)) ?? false;
            }
            """;
        await page.WaitForFunctionAsync(Clear, readings, new() { Timeout = 5000 });
        // The grid holds a scrolled-to row until its rows have been still for a second, so the check waits past that.
        await page.WaitForTimeoutAsync(1500);
        Assert.True(await readings.EvaluateAsync<bool>(Clear));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task VirtualRows_ShouldLeaveTheScrollToThePerson_WhenTheyScrollAwayFromARowScrolledTo(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var table = page.GetByRole(AriaRole.Table, new() { Name = "Readings", Exact = true });
        await Expect(table.Locator("tbody tr[data-part=row]").First).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Go to reading 5000" }).ClickAsync();
        var row = table.Locator("tr[data-key='5000']");
        await Expect(row).ToBeInViewportAsync();

        var box = (await row.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.WheelAsync(0, 3000);

        await Expect(row).Not.ToBeInViewportAsync();
        // The grid holds a scrolled-to row until its rows have been still for a second, so the check waits past that.
        await page.WaitForTimeoutAsync(1500);
        await Expect(row).Not.ToBeInViewportAsync();
    }
}
