namespace Trailblazor.BrowserTests;

public class ButtonGroupTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ButtonGroup_ShouldJoinAButtonAndAMenusTriggerWithOneSeam_WhenTheyAreFilled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button-group");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true });
        var more = page.GetByRole(AriaRole.Button, new() { Name = "More ways to save" });

        var styles = await page.EvaluateAsync<string[]>("""
            () => {
                const [save, more] = [...document.querySelectorAll('[aria-label="Save the trail"] .tb-button')];
                const a = getComputedStyle(save);
                const b = getComputedStyle(more);
                return [a.borderTopRightRadius, b.borderTopLeftRadius, b.borderLeftColor, a.borderRightColor];
            }
            """);
        var saveBox = (await save.BoundingBoxAsync())!;
        var moreBox = (await more.BoundingBoxAsync())!;

        Assert.Equal(("0px", "0px"), (styles[0], styles[1]));
        Assert.NotEqual(styles[3], styles[2]);
        Assert.NotEqual("rgba(0, 0, 0, 0)", styles[2]);
        Assert.Equal(saveBox.X + saveBox.Width - 1, moreBox.X, tolerance: 0.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ButtonGroup_ShouldStretchEveryKindOfItemToTheColumnsWidth_WhenVertical(string browser)
    {
        var page = await OpenAsync(browser, "/button-group");
        var group = (await page.GetByRole(AriaRole.Group, new() { Name = "Map tools" }).BoundingBoxAsync())!;

        foreach (var name in new[] { "Fit", "Layers and labels", "Menu", "Share" })
        {
            var button = (await page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true })
                .BoundingBoxAsync())!;
            Assert.True(Math.Abs(button.Width - group.Width) <= 0.5,
                $"{name} is {button.Width}px in a {group.Width}px column.");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ButtonGroup_ShouldStackTooltipWrappedButtonsAsOneColumn_WhenVertical(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button-group");
        var zoomIn = (await page.GetByRole(AriaRole.Button, new() { Name = "Zoom in" }).BoundingBoxAsync())!;
        var zoomOut = (await page.GetByRole(AriaRole.Button, new() { Name = "Zoom out" }).BoundingBoxAsync())!;

        var radii = await page.EvaluateAsync<string[]>("""
            () => {
                const [zoomIn, zoomOut] = [...document.querySelectorAll('[aria-label="Zoom"] .tb-button')];
                return [getComputedStyle(zoomIn).borderBottomLeftRadius, getComputedStyle(zoomOut).borderTopLeftRadius];
            }
            """);

        Assert.Equal(zoomIn.X, zoomOut.X, tolerance: 0.5);
        Assert.Equal(zoomIn.Y + zoomIn.Height - 1, zoomOut.Y, tolerance: 0.5);
        Assert.Equal(["0px", "0px"], radii);
    }
}
