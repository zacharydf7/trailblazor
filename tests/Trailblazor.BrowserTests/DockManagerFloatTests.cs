using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerFloatTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldFloatAPanelInAWindowOverTheGroupsAndDockItAgain_WhenTheHelpersAreUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        var city = page.FrameLocator("iframe[title=Forecast]").GetByLabel("City");
        await city.FillAsync("Zermatt");
        var whole = (await page.Locator(".tb-dock").BoundingBoxAsync())!;

        await dock.FloatAsync("Weather");

        Assert.Equal(["Weather"], await dock.FloatingAsync());
        await Expect(Tab(page, "Weather")).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Floating panel group 3" })).ToBeVisibleAsync();
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("\"floating\":[{\"panels\":[\"weather\"]");
        // Half of the dock, over its middle, inside the dock's one-pixel border.
        var row = (await page.Locator(".tb-dock__group--floating").BoundingBoxAsync())!;
        var panel = (await Panel(page, "Weather").BoundingBoxAsync())!;
        Assert.Equal(whole.X + 1 + (whole.Width - 2) / 4, row.X, 1.5);
        Assert.Equal(whole.Y + 1 + (whole.Height - 2) / 4, row.Y, 1.5);
        Assert.Equal((whole.Width - 2) / 2, row.Width, 1.5);
        Assert.Equal(whole.Y + 1 + (whole.Height - 2) * 0.75, panel.Y + panel.Height, 1.5);
        // The window is in front of the notes, which are docked under it.
        Assert.True(await Panel(page, "Weather").EvaluateAsync<bool>("""
            panel => {
                const box = panel.getBoundingClientRect();
                return panel.contains(document.elementFromPoint(box.left + box.width / 2, box.top + box.height / 2));
            }
            """));
        // What the page layers above the dock is above its windows too, however high they stack among themselves.
        Assert.True(await Panel(page, "Weather").EvaluateAsync<bool>("""
            panel => {
                const box = panel.getBoundingClientRect();
                const cover = document.createElement('div');
                cover.style.cssText = 'position: fixed; z-index: 5; inset: 0';
                document.body.append(cover);
                const top = document.elementFromPoint(box.left + box.width / 2, box.top + box.height / 2);
                cover.remove();
                return top === cover;
            }
            """));
        // The frame stayed loaded, and the window comes last in reading order.
        await Expect(city).ToHaveValueAsync("Zermatt");
        Assert.EndsWith("group weather", await PartsAsync(page));

        // A separator moves the docked groups under the window, which keeps its box.
        await dock.ResizeAsync("Resize Trails", 40);
        Assert.Equal(row.X, (await page.Locator(".tb-dock__group--floating").BoundingBoxAsync())!.X, 0.5);
        Assert.Equal(row.Width, (await page.Locator(".tb-dock__group--floating").BoundingBoxAsync())!.Width, 0.5);
        // A group docked after the window was made still comes before it in reading order.
        await dock.MoveToNewGroupAsync("Gear", DockPosition.Bottom);
        Assert.EndsWith("group weather", await PartsAsync(page));

        await dock.DockAsync("Weather");

        Assert.Empty(await dock.FloatingAsync());
        await Expect(Tab(page, "Weather")).ToBeFocusedAsync();
        await Expect(page.GetByTestId("layout")).Not.ToContainTextAsync("floating");
        await Expect(city).ToHaveValueAsync("Zermatt");
        Assert.Equal("The panel \"Weather\" is docked already.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.DockAsync("Weather"))).Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldBringAWindowToTheFrontAndKeepTheFocusOnItsTab_WhenATabInItIsPressed(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        await dock.FloatAsync("Map");
        // Three groups stay docked, and the windows are numbered after them, oldest first.
        var gear = page.GetByRole(AriaRole.Tablist, new() { Name = "Floating panel group 4" });
        var map = page.GetByRole(AriaRole.Tablist, new() { Name = "Floating panel group 5" });
        const string layer = "list => list.closest('.tb-dock__group').style.getPropertyValue('--tb-dock-z')";
        Assert.Equal(("0", "1"), (await gear.EvaluateAsync<string>(layer), await map.EvaluateAsync<string>(layer)));
        // The panel on top at the middle of Gear's window, which the map's window covers at first.
        const string onTop = """
            panel => {
                const box = panel.getBoundingClientRect();
                const top = document.elementFromPoint(box.left + box.width / 2, box.top + box.height / 2);
                return top.closest('.tb-dock__panel').dataset.panel;
            }
            """;
        Assert.Equal("map", await Panel(page, "Gear").EvaluateAsync<string>(onTop));

        // The map's window sits a little down and along from Gear's, whose corner still shows.
        await Tab(page, "Gear").ClickAsync(new() { Position = new() { X = 6, Y = 4 } });

        await Expect(gear.Locator("xpath=ancestor::*[@data-part='group']"))
            .ToHaveAttributeAsync("style", new Regex("--tb-dock-z: 1;"));
        Assert.Equal("0", await map.EvaluateAsync<string>(layer));
        Assert.Equal("gear", await Panel(page, "Gear").EvaluateAsync<string>(onTop));
        // The windows keep their numbers, and the tab that was pressed is the same element, still focused.
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
        await Expect(page.GetByTestId("layout")).ToContainTextAsync(
            "\"floating\":[{\"panels\":[\"map\"],\"active\":\"map\",\"x\":28,\"y\":28,\"width\":50,\"height\":50},"
                + "{\"panels\":[\"gear\"]");

        // The focus arriving in the window behind, as with the Tab key, brings that one forward too.
        await Tab(page, "Map").FocusAsync();

        await Expect(map.Locator("xpath=ancestor::*[@data-part='group']"))
            .ToHaveAttributeAsync("style", new Regex("--tb-dock-z: 1;"));
        await Expect(Tab(page, "Map")).ToBeFocusedAsync();
    }
}
