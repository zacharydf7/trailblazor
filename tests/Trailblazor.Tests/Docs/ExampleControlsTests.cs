using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs;

public class ExampleControlsTests : DocsTest
{
    private static readonly ExampleControls Button = new("<TbButton{controls}>Save</TbButton>",
        ExampleControl.Choice("Variant", Variant.Filled), ExampleControl.Switch("Disabled"),
        ExampleControl.Switch("Modal", true));

    [Fact]
    public void MarkupFor_ShouldLeaveOutValuesAsTheyStarted_WhenNoneChanged()
    {
        Assert.Equal("<TbButton>Save</TbButton>", Button.MarkupFor(Button.Initial));
    }

    [Fact]
    public void MarkupFor_ShouldWriteParameters_WhenValuesChanged()
    {
        var values = new Dictionary<string, object?>
        {
            ["Variant"] = Variant.Outlined, ["Disabled"] = true, ["Modal"] = false,
        };

        Assert.Equal("<TbButton Variant=\"Variant.Outlined\" Disabled Modal=\"false\">Save</TbButton>",
            Button.MarkupFor(values));
    }

    [Fact]
    public void MarkupFor_ShouldWriteOptions_WhenTheControlsAreOptions()
    {
        var dialog = new ExampleControls("<TbDialog{controls} />", ExampleControl.Choice("Size", DialogSize.Medium),
            ExampleControl.Switch("CloseButton", true)) { AsOptions = true };
        var values = new Dictionary<string, object?> { ["Size"] = DialogSize.Large, ["CloseButton"] = false };

        Assert.Equal("<TbDialog Options=\"new() { Size = DialogSize.Large, CloseButton = false }\" />",
            dialog.MarkupFor(values));
    }

    [Fact]
    public void MarkupFor_ShouldKeepTextAndEscapeItsQuotes_WhenTheControlIsText()
    {
        var field = new ExampleControls("<TbTextField{controls} />", ExampleControl.Text("Placeholder", "Your name"));

        Assert.Equal("<TbTextField Placeholder=\"Your name\" />", field.MarkupFor(field.Initial));
        Assert.Equal("<TbTextField Placeholder=\"&quot;A&quot;\" />",
            field.MarkupFor(new Dictionary<string, object?> { ["Placeholder"] = "\"A\"" }));
    }

    [Fact]
    public void MarkupFor_ShouldLeaveOutText_WhenItIsEmpty()
    {
        var field = new ExampleControls("<TbTextField{controls} />", ExampleControl.Text("HelperText"),
            ExampleControl.Text("Placeholder", "Your name"));

        Assert.Equal("<TbTextField Placeholder=\"Your name\" />", field.MarkupFor(field.Initial));
        Assert.Equal("<TbTextField />",
            field.MarkupFor(new Dictionary<string, object?> { ["HelperText"] = null, ["Placeholder"] = null }));
    }

    [Theory]
    [InlineData("button"), InlineData("text-field"), InlineData("select"), InlineData("tabs"), InlineData("dialog")]
    public void ExampleCatalog_ShouldOfferControlsFirst_WhenGivenAMainPage(string name)
    {
        Assert.NotNull(ExampleCatalog.Find(name)!.Shown[0].Controls);
    }

    [Fact]
    public async Task ExampleFrame_ShouldUpdateTheDemoAndShowItsMarkup_WhenAControlChanges()
    {
        var cut = ComponentPage("button");

        await cut.Tb().Select("Variant").ChooseAsync("Outlined");
        cut.Find("[role=group][aria-label=Controls] input[role=switch]").Change(true);

        var button = cut.Find(".docs-example__demo button");
        Assert.Contains("tb-button--outlined", button.ClassName);
        Assert.True(button.HasAttribute("disabled"));
        Assert.Contains("<TbButton Variant=\"Variant.Outlined\" Disabled>", cut.Find("#code-try-it").TextContent);
    }

    [Theory]
    [InlineData("text-field", "Disabled", ".tb-field--disabled")]
    [InlineData("select", "Disabled", ".tb-field--disabled"), InlineData("tabs", "Vertical", ".tb-tabs--vertical")]
    public void ExampleFrame_ShouldChangeTheDemoAndShowTheParameter_WhenItsSwitchIsTurnedOn(string page,
        string parameter, string changed)
    {
        var cut = ComponentPage(page);
        var firstDemo = () => cut.FindAll(".docs-example__demo")[0];
        Assert.Null(firstDemo().QuerySelector(changed));

        cut.FindAll("[role=group][aria-label=Controls] input[role=switch]")[0].Change(true);

        Assert.NotNull(firstDemo().QuerySelector(changed));
        Assert.Contains($" {parameter}", cut.Find("#code-try-it").TextContent);
    }

    [Fact]
    public void ExampleFrame_ShouldPassTextToTheDemoAndTakeItBack_WhenATextControlIsFilledAndCleared()
    {
        var cut = ComponentPage("text-field");
        var demo = () => cut.Find(".docs-example__demo input");
        var placeholder = () => cut.FindAll("[role=group][aria-label=Controls] input")[0];

        placeholder().Input("Your trail");
        Assert.Equal("Your trail", demo().GetAttribute("placeholder"));
        Assert.Contains("Placeholder=\"Your trail\"", cut.Find("#code-try-it").TextContent);

        placeholder().Input("");
        Assert.False(demo().HasAttribute("placeholder"));
        Assert.DoesNotContain("Placeholder", cut.Find("#code-try-it").TextContent);
    }

    [Fact]
    public async Task ExampleFrame_ShouldShowTheOptionsMarkup_WhenADialogControlChanges()
    {
        var cut = ComponentPage("dialog");

        await cut.Tb().Select("Size").ChooseAsync("Large");

        Assert.Contains("Options=\"new() { Size = DialogSize.Large }\"", cut.Find("#code-try-it").TextContent);
    }

    [Fact]
    public async Task ExampleFrame_ShouldOpenTheDialogWithTheChosenValues_WhenAControlChanges()
    {
        var cut = ComponentPage("dialog");

        await cut.Tb().Select("Size").ChooseAsync("Large");
        cut.FindAll("[role=group][aria-label=Controls] input[role=switch]")[0].Change(false);
        Button(cut, "Try this dialog").Click();

        var dialog = cut.Find(".docs-example__demo dialog");
        Assert.Contains("tb-dialog--large", dialog.ClassName);
        Assert.Null(dialog.QuerySelector("[data-part=header]"));
    }

    [Fact]
    public void ExampleFrame_ShouldShowTheMarkupForTheChosenValues_WhenCodeIsShown()
    {
        var cut = ComponentPage("button");

        cut.FindAll("[role=group][aria-label=Controls] input[role=switch]")[1].Change(true);
        cut.Find("button[aria-controls='code-try-it']").Click();

        Assert.False(cut.Find("#code-try-it").HasAttribute("hidden"));
        Assert.Contains("<TbButton Loading>Try this button</TbButton>", cut.Find("#code-try-it").TextContent);
    }
}
