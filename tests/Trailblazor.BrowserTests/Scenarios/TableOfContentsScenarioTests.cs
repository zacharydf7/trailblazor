using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TableOfContentsScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PackingGuide_ShouldMarkTheSectionInView_WhenTheNotesScroll(string browser)
    {
        var page = await OpenAsync(browser, "/examples/table-of-contents/active");
        // The contents have no helper of their own: they are navigation, and the link to the section in view has
        // aria-current.
        var contents = page.GetByRole(AriaRole.Navigation, new() { Name = "Packing guide" });
        var inView = page.GetByLabel("Section in view");

        // Scroll the notes to their end, as the wheel would.
        await page.GetByRole(AriaRole.Region, new() { Name = "Packing notes" })
            .EvaluateAsync("notes => notes.scrollTop = notes.scrollHeight");
        await Expect(inView).ToHaveTextAsync("Food");
        var food = contents.GetByRole(AriaRole.Link, new() { Name = "Food" });
        await Expect(food).ToHaveAttributeAsync("aria-current", "location");

        // Following a link scrolls its section into view.
        await contents.GetByRole(AriaRole.Link, new() { Name = "Shelter" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Shelter" })).ToBeInViewportAsync();
        await Expect(inView).ToHaveTextAsync("Shelter");
    }
}
