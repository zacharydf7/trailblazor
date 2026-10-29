using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class DialogServiceTests : BunitContext
{
    public DialogServiceTests()
    {
        this.AddTrailblazor();
    }

    private IDialogService Dialogs => Services.GetRequiredService<IDialogService>();

    [Fact]
    public void ShowAsync_ShouldRenderATitledDialogWithItsParameters_WhenCalled()
    {
        var host = Render<DialogHost>();

        _ = Dialogs.ShowAsync<GreetingDialog, string>("Greeting", p => p.Set(d => d.Name, "Ada"));

        var dialog = host.WaitForElement("dialog");
        Assert.Equal(host.Find("h2").Id, dialog.GetAttribute("aria-labelledby"));
        Assert.Equal("Greeting", host.Find("h2").TextContent);
        Assert.Contains("Hello Ada", host.Find("[data-part=body]").TextContent);
        Assert.Contains("tb-dialog--medium", dialog.ClassList);
        Assert.Null(dialog.GetAttribute("popover"));
    }

    [Fact]
    public async Task CloseAsync_ShouldCompleteWithTheValueAndRemoveTheDialog_WhenTheDialogConfirms()
    {
        var host = Render<DialogHost>();
        var showing = Dialogs.ShowAsync<GreetingDialog, string>("Greeting", p => p.Set(d => d.Name, "Ada"));

        await host.WaitForElement("[data-testid=accept]").ClickAsync(new());

        var result = await showing;
        Assert.True(result.Confirmed);
        Assert.Equal("Ada", result.Value);
        Assert.Empty(host.FindAll("dialog"));
        Assert.Empty(Dialogs.OpenDialogs);
    }

    [Fact]
    public async Task CloseButton_ShouldCancelTheDialog_WhenClicked()
    {
        var host = Render<DialogHost>();
        var showing = Dialogs.ShowAsync<GreetingDialog, string>("Greeting");

        await host.WaitForElement("[data-part=close]").ClickAsync(new());

        var result = await showing;
        Assert.False(result.Confirmed);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldReturnTrue_WhenTheConfirmButtonIsClicked()
    {
        var host = Render<DialogHost>();
        var confirming = Dialogs.ConfirmAsync("Delete?", "This can't be undone.", "Delete", tone: Tone.Danger);

        var buttons = host.WaitForElements("[data-part=actions] button", 2);
        Assert.Contains("This can't be undone.", host.Find("[data-part=body]").TextContent);
        Assert.Contains("tb-button--danger", buttons[1].ClassList);
        await buttons[1].ClickAsync(new());

        Assert.True(await confirming);
    }

    [Fact]
    public void ShowAsync_ShouldListDialogsNewestLast_WhenTwoAreOpen()
    {
        Render<DialogHost>();

        _ = Dialogs.ShowAsync<GreetingDialog, string>("First");
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Second");

        Assert.Equal(["First", "Second"], Dialogs.OpenDialogs.Select(dialog => dialog.Title));
    }

    [Fact]
    public void ShowAsync_ShouldNameTheDialogWithoutAHeader_WhenShowHeaderIsOff()
    {
        var host = Render<DialogHost>();

        _ = Dialogs.ShowAsync<GreetingDialog, string>("Greeting", options: new() { ShowHeader = false, Modal = false });

        var dialog = host.WaitForElement("dialog");
        Assert.Equal("Greeting", dialog.GetAttribute("aria-label"));
        Assert.Empty(host.FindAll("header"));
        Assert.Equal("manual", dialog.GetAttribute("popover"));
    }

    [Fact]
    public void Set_ShouldThrow_WhenGivenAnExpressionThatIsNotAProperty()
    {
        var parameters = new DialogParameters<GreetingDialog>();

        Assert.Throws<ArgumentException>(() => parameters.Set(d => d.Name + "!", "Ada"));
    }

    [Fact]
    public void Set_ShouldThrow_WhenGivenANestedPropertyOrOneThatIsNotAParameter()
    {
        var parameters = new DialogParameters<CountingDialog>();

        Assert.Throws<ArgumentException>(() => parameters.Set(d => d.Settings!.Theme, "dark"));
        Assert.Throws<ArgumentException>(() => parameters.Set(d => d.ParametersSet, 1));
    }

    [Fact]
    public void ShowAsync_ShouldNotReapplyAnOpenDialogsParameters_WhenAnotherDialogOpens()
    {
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<CountingDialog, bool>("Edit", p => p.Set(d => d.Settings, new Settings()));
        var editing = Dialogs.OpenDialogs.Single();
        var counter = host.FindComponent<CountingDialog>().Instance;
        var before = counter.ParametersSet;

        _ = Dialogs.ShowAsync<GreetingDialog, string>("Discard changes?");

        Assert.Equal(before, counter.ParametersSet);
        Assert.Contains(editing, Dialogs.OpenDialogs);
    }

    [Fact]
    public async Task Dispose_ShouldCleanUpTheBrowserDialog_WhenTheHostGoesAwayWithADialogOpen()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/dialog.js").SetupModule("openDialog", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Greeting");
        host.WaitForElement("dialog");

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }

    [Fact]
    public async Task CloseAsync_ShouldStillCompleteAndRemoveTheDialog_WhenTheBrowserCloseFails()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/dialog.js").SetupModule("openDialog", _ => true);
        handle.SetupVoid("close").SetException(new Microsoft.JSInterop.JSException("The browser refused."));
        var host = Render<DialogHost>();
        var showing = Dialogs.ShowAsync<GreetingDialog, string>("Greeting", p => p.Set(d => d.Name, "Ada"));

        await Assert.ThrowsAsync<Microsoft.JSInterop.JSException>(
            () => host.WaitForElement("[data-testid=accept]").ClickAsync(new()));

        Assert.True(showing.IsCompleted);
        Assert.True((await showing).Confirmed);
        Assert.Empty(Dialogs.OpenDialogs);
    }

    [Fact]
    public void ConfirmAsync_ShouldBeAnAlertDialogDescribedByItsMessage_WhenCalled()
    {
        var host = Render<DialogHost>();

        _ = Dialogs.ConfirmAsync("Delete?", "This can't be undone.");

        var dialog = host.WaitForElement("dialog");
        Assert.Equal("alertdialog", dialog.GetAttribute("role"));
        var description = host.Find($"#{dialog.GetAttribute("aria-describedby")}");
        Assert.Equal("This can't be undone.", description.TextContent.Trim());
    }

    [Fact]
    public void CloseAsync_ShouldThrow_WhenTheDialogWasNotOpenedByTheService()
    {
        var cut = Render<GreetingDialog>();

        Assert.Throws<InvalidOperationException>(() => cut.Find("[data-testid=accept]").Click());
    }

    private sealed class Settings
    {
        public string Theme { get; set; } = "light";
    }

    private sealed class CountingDialog : TbDialog<bool>
    {
        [Parameter]
        public Settings? Settings { get; set; }

        public int ParametersSet { get; private set; }

        protected override void OnParametersSet() => ParametersSet++;
    }

    private sealed class GreetingDialog : TbDialog<string>
    {
        [Parameter]
        public string? Name { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"Hello {Name}");
            builder.OpenElement(1, "button");
            builder.AddAttribute(2, "data-testid", "accept");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => CloseAsync(Name ?? "")));
            builder.CloseElement();
        }
    }
}
