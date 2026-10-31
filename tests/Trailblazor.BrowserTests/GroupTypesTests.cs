namespace Trailblazor.BrowserTests;

public class GroupTypesTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Items_ShouldSetTheirGroupsValue_WhenGivenPlainValuesInANullableOrEnumGroup(string browser)
    {
        var page = await OpenAsync(browser, "/group-types");

        await page.GetByRole(AriaRole.Radio, new() { Name = "Any" }).CheckAsync();
        await Expect(page.GetByTestId("size")).ToBeEmptyAsync();
        await page.GetByRole(AriaRole.Radio, new() { Name = "Four" }).CheckAsync();
        await page.GetByRole(AriaRole.Radio, new() { Name = "Brisk" }).CheckAsync();
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Day two" }).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Stops" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitemradio, new() { Name = "Three stops" }).ClickAsync();

        await Expect(page.GetByTestId("size")).ToHaveTextAsync("4");
        await Expect(page.GetByTestId("pace")).ToHaveTextAsync("Brisk");
        await Expect(page.GetByTestId("days")).ToHaveTextAsync("2");
        await Expect(page.GetByTestId("stops")).ToHaveTextAsync("3");
    }
}
