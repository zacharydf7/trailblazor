using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

// The locator each docs page shows under Testing has to find the component in that page's example.
public class ExampleTargetTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    public static TheoryData<string, string> TargetedExamples => new(
        from browser in AllBrowsers.Names
        from example in ExampleCatalog.All
        where example.Target is not null
        select (browser, example.Name));

    [Theory, MemberData(nameof(TargetedExamples))]
    public async Task Target_ShouldFindTheComponentInItsExample_WhenFollowingItsSteps(string browser, string example)
    {
        var target = ExampleCatalog.Find(example)!.Target!;
        var page = await OpenAsync(browser, $"/examples/{example}");

        if (target.OpenedBy is { } opener)
        {
            var trigger = Find(page, opener);
            await (target.OpenOnHover ? trigger.HoverAsync() : trigger.ClickAsync());
        }

        // Strict, as a test that copies the locator would be: one visible match, not several.
        await Expect(Find(page, target)).ToBeVisibleAsync();
    }

    private static ILocator Find(IPage page, TestTarget target)
    {
        var role = Enum.Parse<AriaRole>(target.Role, ignoreCase: true);
        return target.Text is null
            ? page.GetByRole(role, new() { Name = target.Name })
            : page.GetByRole(role).Filter(new() { HasText = target.Text });
    }
}
