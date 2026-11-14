using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// The Playwright helpers for checkboxes, switches, groups, lists and text fields, which share their names with bUnit's.
public class DriverChoicesTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Checkbox_ShouldCheckAndUncheckItOnceInteractive_WhenThePageIsStillPrerendered(string browser)
    {
        var (page, held) = await OpenHeldAsync(browser, "/examples/checkbox");
        var updates = page.Tb().Checkbox("Email me updates");

        var checking = updates.CheckAsync();
        await page.WaitForTimeoutAsync(300);
        held.SetResult();
        await checking;
        Assert.True(await updates.CheckedAsync());
        Assert.Equal(1, await page.GetByText("Updates value: True", new() { Exact = true }).CountAsync());
        await updates.UncheckAsync();

        Assert.Equal(1, await page.GetByText("Updates value: False", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldUseTheNativeInputsAndRefuseWhatAUserCantDo_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-choices");

        await page.Tb().Checkbox("Email me updates").CheckAsync();
        await page.Tb().RadioGroup("Paper size").ChooseAsync("A4");
        await page.Tb().TextField("Name").EnterAsync("Ada");

        Assert.True(await page.Tb().Checkbox("Email me updates").CheckedAsync());
        Assert.Equal("A4", await page.Tb().RadioGroup("Paper size").ChosenAsync());
        Assert.Equal("Ada", await page.Tb().TextField("Name").TextAsync());
        Assert.Equal(["Trails"], await page.Tb().ToggleGroup("Map view").ChosenAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().Checkbox("Locked in").UncheckAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().ToggleGroup("Map view").ChooseAsync("Trails"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Groups_ShouldNameOptionsByTheirOwnLabel_WhenOptionsHoldRichContent(string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox-group");
        var radios = await OpenAsync(browser, "/examples/radio-group");

        Assert.Single(await page.Tb().CheckboxGroup("Gear to carry").CheckedAsync());
        Assert.StartsWith("Season pass", await radios.Tb().RadioGroup("Membership").ChosenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldReleaseItemsOnlyWhereTheGroupAllows_WhenUnchoosing(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toggle-group");
        await page.Tb().ToggleGroup("Seasons open").ChooseAsync("Spring", "Fall");
        await page.Tb().ToggleGroup("Distance units").ChooseAsync("Miles");

        await page.Tb().ToggleGroup("Seasons open").UnchooseAsync("Spring");
        await page.Tb().ToggleGroup("Distance units").UnchooseAsync("Miles");

        // Summer starts pressed.
        Assert.Equal(["Summer", "Fall"], await page.Tb().ToggleGroup("Seasons open").ChosenAsync());
        await Expect(page.GetByText("Units: none", new() { Exact = true })).ToBeVisibleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().ToggleGroup("Map view").UnchooseAsync("Trails"));
        await Assert.ThrowsAsync<ArgumentException>(
            () => page.Tb().ToggleGroup("Map view").ChooseAsync("Trails", "Satellite"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Switch_ShouldTurnItOn_WhenTheHelperIsUsed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/switch");

        await page.Tb().Switch("Auto-save").CheckAsync();

        Assert.True(await page.Tb().Switch("Auto-save").CheckedAsync());
        Assert.Equal(1, await page.GetByText("Auto-save value: True", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CheckboxGroup_ShouldCheckAndUncheckOptionsByName_WhenTheHelperIsUsed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox-group");
        var days = page.Tb().CheckboxGroup("Trail days");

        await days.CheckAsync("Monday", "Friday");
        await days.UncheckAsync("Monday");

        // Saturday starts checked.
        Assert.Equal(["Friday", "Saturday"], await days.CheckedAsync());
        Assert.Equal(1,
            await page.GetByText("Trail days value: Saturday, Friday", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RadioGroup_ShouldChooseAnOptionAndRefuseADisabledOne_WhenTheHelperIsUsed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/radio-group");
        var paper = page.Tb().RadioGroup("Paper size");

        await paper.ChooseAsync("A4");

        Assert.Equal("A4", await paper.ChosenAsync());
        Assert.Equal(1, await page.GetByText("Paper size value: A4", new() { Exact = true }).CountAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => paper.ChooseAsync("Legal (out of stock)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldPressAnItem_WhenTheHelperIsUsed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toggle-group");

        await page.Tb().ToggleGroup("Map view").ChooseAsync("Satellite");

        Assert.Equal(["Satellite"], await page.Tb().ToggleGroup("Map view").ChosenAsync());
        await Expect(page.GetByText("View: Satellite", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task List_ShouldReadTheItemsAndClickOne_WhenTheHelperIsUsed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/list");
        var sites = page.Tb().List("Campsites");

        Assert.Equal(["Pine Hollow", "Eagle Ridge", "Cedar Flats"], (await sites.ItemsAsync()).Take(3));
        await sites.ClickAsync("Eagle Ridge");

        await Expect(page.GetByText("Showing: Eagle Ridge", new() { Exact = true })).ToBeVisibleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sites.ClickAsync("Cedar Flats"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldTypeTheTextOnceInteractive_WhenThePageIsStillPrerendered(string browser)
    {
        var (page, held) = await OpenHeldAsync(browser, "/examples/text-field");

        var typing = page.Tb().TextField("Name").EnterAsync("Ada");
        await page.WaitForTimeoutAsync(300);
        held.SetResult();
        await typing;

        Assert.Equal("Ada", await page.Tb().TextField("Name").TextAsync());
        Assert.Equal(1, await page.GetByText("Name: Ada", new() { Exact = true }).CountAsync());
    }

    // The page stays prerendered until Blazor's script is let through, which the test does once the helper is waiting.
    private async Task<(IPage Page, TaskCompletionSource Held)> OpenHeldAsync(string browser, string path)
    {
        var held = new TaskCompletionSource();
        var page = await OpenStaticAsync(browser, path);
        await page.RouteAsync("**/_framework/blazor.web*.js", async route =>
        {
            await held.Task;
            await route.ContinueAsync();
        });
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Commit });
        return (page, held);
    }
}
