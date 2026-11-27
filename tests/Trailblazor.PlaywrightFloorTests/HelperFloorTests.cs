using Trailblazor.Testing.Playwright;

namespace Trailblazor.PlaywrightFloorTests;

// Smoke-tests a few helpers of each kind, calling only the helpers and Playwright APIs the package's minimum already
// had. Each check reads the page once, without retrying, so a helper that returns before the app has answered fails.
public class HelperFloorTests(FloorFixture fixture)
{
    [Fact]
    public async Task Choices_ShouldReachTheApp_WhenCheckedOrChosen()
    {
        var boxes = await fixture.OpenAsync("/examples/checkbox");
        var radios = await fixture.OpenAsync("/examples/radio-group");

        await boxes.Tb().Checkbox("Email me updates").CheckAsync();
        await radios.Tb().RadioGroup("Paper size").ChooseAsync("A4");

        Assert.Equal(1, await boxes.GetByText("Updates value: True", new() { Exact = true }).CountAsync());
        Assert.Equal(1, await radios.GetByText("Paper size value: A4", new() { Exact = true }).CountAsync());
    }

    // Not a helper test, but only this project runs the old Chromium, which crashed on a color derived from a plain
    // one, such as an app sets inline. The buttons show every tone, so reading their styles computes each derived
    // color.
    [Fact]
    public async Task Styles_ShouldKeepThePageWorking_WhenTheAppSetsItsColorsPlain()
    {
        var page = await fixture.OpenAsync("/examples/button");

        var styled = await page.EvaluateAsync<int>("""
            () => {
                for (const tone of ['primary', 'secondary', 'success', 'warning', 'danger', 'info']) {
                    document.documentElement.style.setProperty(`--tb-color-${tone}`, '#0f766e');
                }

                return [...document.querySelectorAll('button')]
                    .filter(button => getComputedStyle(button).backgroundColor !== '').length;
            }
            """);

        Assert.True(styled > 0);
        Assert.True(await page.EvaluateAsync<bool>(
            "() => new Promise(drawn => requestAnimationFrame(() => drawn(true)))"));
    }

    [Fact]
    public async Task Form_ShouldListItsErrors_WhenSubmittedEmpty()
    {
        var page = await fixture.OpenAsync("/examples/form");

        await page.Tb().Form("Trip sign-up").SubmitAsync();

        Assert.Equal(3, (await page.Tb().Form("Trip sign-up").ErrorsAsync()).Count);
    }

    [Fact]
    public async Task Dialog_ShouldAnswerTheApp_WhenAButtonIsChosen()
    {
        var page = await fixture.OpenAsync("/examples/dialog");

        await page.Tb().Button("Delete report").ClickAsync();
        await page.Tb().Dialog("Delete report?").ChooseAsync("Delete");

        Assert.Equal(1, await page.GetByText("Deleted", new() { Exact = true }).CountAsync());
    }

    [Fact]
    public async Task Speech_ShouldReachTheApp_WhenSaid()
    {
        var page = await fixture.OpenAsync("/examples/speech-to-text");
        await page.UseFakeSpeechAsync();
        await page.ReloadAsync();
        var dictate = page.Tb().SpeechToText("Dictate notes");

        await dictate.StartAsync();
        await dictate.SayAsync("Bridge out at mile three.");

        Assert.Equal("Bridge out at mile three.",
            await page.GetByRole(AriaRole.Textbox, new() { Name = "Trail notes" }).InputValueAsync());
    }
}
