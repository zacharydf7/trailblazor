namespace Trailblazor.BrowserTests;

public class StylesheetCheckTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Warning = "outside a cascade layer";

    private const string ImportedReset = "url('data:text/css,button{border-radius:0}')";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Root_ShouldWarnInTheConsole_WhenAStylesheetOutsideALayerStylesButtons(string browser)
    {
        var page = await OpenStaticAsync(browser, "about:blank");

        await page.RunAndWaitForConsoleMessageAsync(
            () => page.GotoAsync(new Uri(Fixture.BaseAddress, "/stylesheet-check").ToString()),
            new() { Predicate = message => message.Type == "warning" && message.Text.Contains(Warning) });

        Assert.EndsWith("Stylesheets: a <style> element", Assert.Single(await WarningsAsync(page)).Text);
    }

    [Theory]
    [InlineData("@layer reset { button { border-radius: 0; } } h1:focus { outline: none; }", 0)]
    [InlineData("@media screen { label { display: inline-block; margin-bottom: .5rem; } }", 1)]
    [InlineData("@container (min-width: 1px) { h1 { margin: 0; } }", 1)]
    [InlineData("h2, .title { font-size: 2rem; }", 1)]
    [InlineData("button { background: none; border: 0; }", 1)]
    [InlineData(":where(button, input) { margin: 0; }", 1)]
    [InlineData(":where(.prose) :where(h2, h3) { margin: 0; }", 0)]
    [InlineData(":is(.card, .panel) :is(h2, h3) { margin: 0; }", 0)]
    [InlineData("*:not(dialog) { margin: 0; }", 1)]
    [InlineData("* { margin: 0; padding: 0; }", 1)]
    [InlineData("*, *::before, *::after { box-sizing: border-box; }", 0)]
    [InlineData("h1 { color: navy; }", 0)]
    [InlineData($"@import {ImportedReset} layer(reset);", 0)]
    [InlineData($"@import {ImportedReset};", 1)]
    public async Task CheckStylesheets_ShouldWarnOnlyAboutRulesThatOverrideTrailblazor_WhenGivenAStylesheet(
        string css, int warnings)
    {
        var page = await OpenAsync("chromium", "/tokens");

        // Only this call's warnings count, so the page's own check, which may run at any time, can't add one.
        var found = await page.EvaluateAsync<string[]>("""
            async css => {
                const { checkStylesheets } = await import('/_content/Trailblazor/stylesheet-check.js');
                const style = document.createElement('style');
                const loaded = new Promise(resolve => style.onload = resolve);
                style.textContent = css;
                document.head.appendChild(style);
                await loaded;
                const warnings = [];
                const warn = console.warn;
                console.warn = message => warnings.push(message);
                checkStylesheets();
                console.warn = warn;
                style.remove();
                return warnings;
            }
            """, css);

        Assert.Equal(warnings, found.Length);
        Assert.All(found, warning => Assert.DoesNotContain(css.StartsWith("@import", StringComparison.Ordinal)
            ? "a <style> element" : "data:text/css", warning, StringComparison.Ordinal));
    }

    private static async Task<IEnumerable<IConsoleMessage>> WarningsAsync(IPage page) =>
        (await page.ConsoleMessagesAsync())
            .Where(message => message.Type == "warning" && message.Text.Contains(Warning));
}
