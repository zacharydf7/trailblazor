using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridBatchTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Changes_ShouldBeMarkedThenSavedTogether_WhenEditedFromTheKeyboard(string browser)
    {
        var page = await OpenReadyAsync(browser);
        // Found by its row's key, since the edit changes the text the grid's helpers find a row by.
        var name = page.Locator("tr[data-key='1'] td[data-column=Name]");

        await name.FocusAsync();
        await page.Keyboard.TypeAsync("Pine loop");
        await Expect(name.Locator("input")).ToHaveValueAsync("Pine loop");
        await page.Keyboard.PressAsync("Enter");
        await Expect(name).ToHaveAttributeAsync("data-changed", "");
        await Expect(name).ToBeFocusedAsync();
        // WebKit doesn't focus a button it clicks, so the keyboard presses Save changes.
        await page.GetByRole(AriaRole.Button, new() { Name = "Save changes" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByRole(AriaRole.Status, new() { Name = "Saved", Exact = true })).ToHaveTextAsync("Pine loop");
        await Expect(page.Locator("td[data-changed]")).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Save changes" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Discard_ShouldShowTheValuesAgain_WhenPressedWithTheKeyboard(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var name = page.Locator("tr[data-key='2'] td[data-column=Name]");
        await name.FocusAsync();
        await page.Keyboard.TypeAsync("Eagle loop");
        await Expect(name.Locator("input")).ToHaveValueAsync("Eagle loop");
        await page.Keyboard.PressAsync("Enter");
        await Expect(name).ToHaveAttributeAsync("data-changed", "");

        await page.GetByRole(AriaRole.Button, new() { Name = "Discard changes" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(name).ToHaveTextAsync("Eagle Ridge");
        await Expect(page.Locator("[data-part=change-count]")).ToHaveTextAsync("Unsaved changes: 0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Edit_ShouldBeKeptAsAChange_WhenFocusLeavesTheCell(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var name = page.Locator("tr[data-key='1'] td[data-column=Name]");
        await name.FocusAsync();
        await page.Keyboard.TypeAsync("Pine loop");
        await Expect(name.Locator("input")).ToHaveValueAsync("Pine loop");

        await page.Locator("tr[data-key='2'] td[data-column=Name]").ClickAsync();

        await Expect(name).ToHaveAttributeAsync("data-changed", "");
        await Expect(page.Locator("[data-part=change-count]")).ToHaveTextAsync("Unsaved changes: 1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldSaveAndDiscardTheChanges_WhenCalled(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        await EditAsync(page, 1, "Pine loop");

        await grid.SaveChangesAsync();
        await Expect(page.GetByRole(AriaRole.Status, new() { Name = "Saved", Exact = true })).ToHaveTextAsync("Pine loop");
        await EditAsync(page, 2, "Eagle loop");
        await grid.DiscardChangesAsync();

        await Expect(page.Locator("tr[data-key='2'] td[data-column=Name]")).ToHaveTextAsync("Eagle Ridge");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SaveChangesAsync_ShouldWaitForTheRetry_WhenAnEarlierSaveWasTurnedDown(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        var attempts = page.GetByRole(AriaRole.Status, new() { Name = "Attempts", Exact = true });
        await EditAsync(page, 1, "Refused");
        await grid.SaveChangesAsync();
        Assert.Equal("1", await attempts.TextContentAsync());

        await grid.SaveChangesAsync();

        Assert.Equal("2", await attempts.TextContentAsync());
        await Expect(page.Locator("[data-part=batch-error]")).ToHaveTextAsync("The server turned the changes down.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SaveChangesAsync_ShouldReturnFromTheRetry_WhenTheAppTurnsTheChangesDownAtOnce(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        var attempts = page.GetByRole(AriaRole.Status, new() { Name = "Attempts", Exact = true });
        await EditAsync(page, 1, "Held");
        await grid.SaveChangesAsync();
        Assert.Equal("1", await attempts.TextContentAsync());

        await grid.SaveChangesAsync();

        Assert.Equal("2", await attempts.TextContentAsync());
        await Expect(page.Locator("[data-part=batch-error]")).ToHaveTextAsync("The server turned the changes down.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SaveChangesAsync_ShouldReturnWithTheEditOpen_WhenTheOpenEditFailsItsCheck(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await EditAsync(page, 1, "Pine loop");
        var name = await OpenEmptyNameAsync(page, 2);

        await page.Tb().Grid("Hikes").SaveChangesAsync();

        await Expect(name.Locator("input")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.GetByRole(AriaRole.Status, new() { Name = "Saved", Exact = true })).ToHaveTextAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SaveChangesAsync_ShouldReturnWithTheEditOpen_WhenTheOpenEditFailsItsCheckAfterARefusal(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        await EditAsync(page, 1, "Held");
        await grid.SaveChangesAsync();
        var name = await OpenEmptyNameAsync(page, 2);

        await grid.SaveChangesAsync();

        await Expect(name.Locator("input")).ToHaveAttributeAsync("aria-invalid", "true");
        Assert.Equal("1", await page.GetByRole(AriaRole.Status, new() { Name = "Attempts", Exact = true }).TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SaveChangesAsync_ShouldWaitForTheSave_WhenAFailedEditWasCorrected(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");
        await EditAsync(page, 1, "Pine loop");
        var name = await OpenEmptyNameAsync(page, 2);
        await grid.SaveChangesAsync();
        await name.Locator("input").FillAsync("Eagle loop");

        await grid.SaveChangesAsync();

        Assert.Equal("Pine loop, Eagle loop",
            await page.GetByRole(AriaRole.Status, new() { Name = "Saved", Exact = true }).TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldThrow_WhenTheGridHoldsNoChanges(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.Tb().Grid("Hikes");

        await Assert.ThrowsAsync<InvalidOperationException>(grid.SaveChangesAsync);
        await Assert.ThrowsAsync<InvalidOperationException>(grid.DiscardChangesAsync);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldSayTheGridHasNoBatchBar_WhenItDoesNotEditInBatches(string browser)
    {
        var page = await OpenAsync(browser, "/grid-keys");
        var grid = page.Tb().Grid("Stops in order");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(grid.SaveChangesAsync);

        Assert.Equal("The grid has no batch bar; set its EditMode to Batch.", error.Message);
    }

    // Opens the edit of a row's name, found by its key, and empties it, which fails the name's check.
    private static async Task<ILocator> OpenEmptyNameAsync(IPage page, int key)
    {
        var name = page.Locator($"tr[data-key='{key}'] td[data-column=Name]");
        await name.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await name.Locator("input").FillAsync("");
        return name;
    }

    // Types over a row's name, found by its key, and saves the cell's edit as a change.
    private static async Task EditAsync(IPage page, int key, string text)
    {
        var name = page.Locator($"tr[data-key='{key}'] td[data-column=Name]");
        await name.FocusAsync();
        await page.Keyboard.TypeAsync(text);
        await Expect(name.Locator("input")).ToHaveValueAsync(text);
        await page.Keyboard.PressAsync("Enter");
        await Expect(name).ToHaveAttributeAsync("data-changed", "");
    }

    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/grid-batch");
        await Expect(page.Locator("[data-part=grid]")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }
}
