using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

// The toolbar, insert divider, theme picker, splitter, chip, image and text pages' examples, as the docs show them.
public class ContentExampleTests : DocsTest
{
    [Fact]
    public void InsertDividerExample_ShouldAddAStepInPlace_WhenADividerIsPressed()
    {
        var cut = Render<InsertDividerExample>();

        Button(cut, "Add a step after Pack the tent").Click();

        Assert.Equal(["Pack the tent", "New step 1", "Fill the water bottles", "Check the trail map"],
            cut.FindAll("ol > li p").Select(step => step.TextContent));
    }

    [Fact]
    public void SplitterPersistExample_ShouldPassItsKey_WhenRendered()
    {
        var cut = Render<SplitterPersistExample>();

        Assert.Equal("docs-splitter-example", cut.FindComponent<TbSplitter>().Instance.PersistenceKey);
    }

    [Fact]
    public void ThemePickerCustomExample_ShouldSetTheMode_WhenTheSwitchIsTurnedOn()
    {
        var cut = Render<ThemePickerCustomExample>();

        cut.Find("input[role=switch]").Change(true);

        Assert.Equal(ThemeMode.Dark, Services.GetRequiredService<IThemeService>().Mode);
    }

    [Fact]
    public void ImageEventsExample_ShouldSayWhatHappened_WhenImagesLoadAndFail()
    {
        var cut = Render<ImageEventsExample>();
        var images = cut.FindAll("img");

        images[0].TriggerEvent("onload", new ProgressEventArgs());
        cut.FindAll("img")[1].TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

        Assert.Equal("Loaded: True. Failed: True.", cut.Find("output").TextContent);
    }
    [Fact]
    public void ChipActionExample_ShouldRunItsClick_WhenTheButtonChipIsPressed()
    {
        var cut = Render<ChipActionExample>();

        Button(cut, "Open the filters").Click();

        Assert.Contains("Times opened: 1", cut.Find("output").TextContent);
    }

    [Fact]
    public void ChipRemoveLabelExample_ShouldUseItsOwnName_WhenAChipIsRemoved()
    {
        var cut = Render<ChipRemoveLabelExample>();

        cut.Find("button[aria-label='Take Ada off the trip']").Click();

        Assert.Empty(cut.FindAll("button[aria-label='Take Ada off the trip']"));
    }

    [Fact]
    public void ChipDisabledExample_ShouldTurnOffItsButtons_WhenRendered()
    {
        var cut = Render<ChipDisabledExample>();

        var buttons = cut.FindAll("button");

        Assert.NotEmpty(buttons);
        Assert.All(buttons, button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public void ThemePickerCustomExample_ShouldShowTheRememberedMode_WhenThePageIsInteractive()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        JSInterop.SetupModule("./_content/Trailblazor/theme.js").Setup<string?[]?>("read")
            .SetResult(["dark", null, null]);

        var cut = Render<ThemePickerCustomExample>();

        cut.WaitForAssertion(() => Assert.True(cut.Find("input[role=switch]").HasAttribute("checked")));
    }

    [Fact]
    public void TextElementExample_ShouldSeparateTheLookFromTheElement_WhenRendered()
    {
        var cut = Render<TextElementExample>();

        Assert.Contains("still a heading", cut.Find("h3").TextContent);
        Assert.Single(cut.FindAll("h1, h2, h3, h4, h5, h6"));
    }

    [Fact]
    public void TextExample_ShouldLeaveThePagesOnlyH1Alone_WhenRendered()
    {
        var cut = Render<TextExample>();

        Assert.Empty(cut.FindAll("h1"));
    }

    [Theory]
    [InlineData("toolbar", "Home"), InlineData("toolbar", "OverflowLabel"), InlineData("insert-divider", "touch")]
    [InlineData("splitter", "arrows"), InlineData("chip", "Delete"), InlineData("image", "Alt")]
    [InlineData("text", "TextStyle")]
    public void ComponentPage_ShouldKeepFeaturesOutOfAccessibility_WhenShowingAPage(string example, string feature)
    {
        var cut = ComponentPage(example);

        Assert.DoesNotContain(feature, cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
