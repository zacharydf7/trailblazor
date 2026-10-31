using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbDialogTests : BunitContext
{
    public TbDialogTests() => this.AddTrailblazor();

    private IDialogService Dialogs => Services.GetRequiredService<IDialogService>();

    [Fact]
    public void Open_ShouldRenderTheDialogWhereItIsWithThePagesCascade_WhenTrue()
    {
        var host = Render<DialogHost>();

        var cut = Render<CascadingValue<string>>(parameters => parameters.Add(value => value.Value, "Ada")
            .AddChildContent<TbDialog>(dialog => dialog.Add(d => d.Open, true).Add(d => d.Title, "Rename")
                .Add(d => d.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenComponent<CascadeReader>(0);
                    builder.CloseComponent();
                }))));

        Assert.Equal("Rename", cut.Find("dialog h2").TextContent);
        Assert.Equal("Hello Ada", cut.Find("[data-part=body]").TextContent.Trim());
        Assert.Equal("Rename", Assert.Single(Dialogs.OpenDialogs).Title);
        Assert.Empty(host.FindAll("dialog"));
    }

    [Fact]
    public void Open_ShouldCloseTheDialogWithoutRaisingOpenChanged_WhenTheParentSetsItFalse()
    {
        var changes = new List<bool>();
        var cut = Render<TbDialog>(parameters => parameters.Add(d => d.Open, true).Add(d => d.Title, "Rename")
            .Add(d => d.OpenChanged, (bool open) => changes.Add(open)));

        cut.Render(parameters => parameters.Add(d => d.Open, false));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("dialog")));
        Assert.Empty(Dialogs.OpenDialogs);
        Assert.Empty(changes);
    }

    [Fact]
    public async Task Open_ShouldOpenANewDialogWithoutRaisingOpenChanged_WhenSetTrueAgainWhileTheDialogFadesOut()
    {
        // The browser's close waits for the fade, so it stays pending until the test finishes it.
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        module.Mode = JSRuntimeMode.Loose;
        var handle = module.SetupModule("openDialog", _ => true);
        handle.Mode = JSRuntimeMode.Loose;
        var fading = handle.SetupVoid("close");
        var changes = new List<bool>();
        var cut = Render<TbDialog>(parameters => parameters.Add(d => d.Open, true).Add(d => d.Title, "Rename")
            .Add(d => d.OpenChanged, (bool open) => changes.Add(open)));
        var first = (DialogReference<bool>)Assert.Single(Dialogs.OpenDialogs);

        cut.Render(parameters => parameters.Add(d => d.Open, false));
        cut.Render(parameters => parameters.Add(d => d.Open, true));
        await cut.InvokeAsync(fading.SetVoidResult);
        await first.Result;

        cut.WaitForAssertion(() => Assert.NotSame(first, Assert.Single(Dialogs.OpenDialogs)));
        Assert.Single(cut.FindAll("dialog"));
        Assert.Empty(changes);
    }

    [Fact]
    public async Task Dispose_ShouldNotOpenANewDialog_WhenTheComponentGoesAwayWhileItsReopenWaitsForTheFade()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        module.Mode = JSRuntimeMode.Loose;
        var handle = module.SetupModule("openDialog", _ => true);
        handle.Mode = JSRuntimeMode.Loose;
        var fading = handle.SetupVoid("close");
        var cut = Render<TbDialog>(parameters => parameters.Add(d => d.Open, true).Add(d => d.Title, "Rename"));
        var first = (DialogReference<bool>)Assert.Single(Dialogs.OpenDialogs);
        cut.Render(parameters => parameters.Add(d => d.Open, false));
        cut.Render(parameters => parameters.Add(d => d.Open, true));

        await DisposeComponentsAsync();
        await cut.InvokeAsync(fading.SetVoidResult);
        await first.Result;

        Assert.Empty(Dialogs.OpenDialogs);
    }

    [Fact]
    public async Task CloseButton_ShouldRaiseOpenChangedWithFalse_WhenTheUserDismissesTheDialog()
    {
        var changes = new List<bool>();
        var cut = Render<TbDialog>(parameters => parameters.Add(d => d.Open, true).Add(d => d.Title, "Rename")
            .Add(d => d.OpenChanged, (bool open) => changes.Add(open)));

        await cut.Find("[data-part=close]").ClickAsync(new());

        cut.WaitForAssertion(() => Assert.Equal([false], changes));
        Assert.Empty(cut.FindAll("dialog"));
    }

    [Fact]
    public async Task CloseButton_ShouldKeepTheDialogOpen_WhenCanDismissRefuses()
    {
        var asked = new List<DialogDismissReason>();
        var cut = Render<TbDialog>(parameters => parameters.Add(d => d.Open, true).Add(d => d.Title, "Rename")
            .Add(d => d.CanDismiss, reason =>
            {
                asked.Add(reason);
                return Task.FromResult(false);
            }));

        await cut.Find("[data-part=close]").ClickAsync(new());

        Assert.Equal([DialogDismissReason.CloseButton], asked);
        Assert.Single(cut.FindAll("dialog"));
    }

    [Fact]
    public void Title_ShouldRenameTheOpenDialog_WhenTheParentChangesIt()
    {
        var cut = Render<TbDialog>(parameters => parameters.Add(d => d.Open, true).Add(d => d.Title, "Rename"));

        cut.Render(parameters => parameters.Add(d => d.Title, "Rename report"));

        cut.WaitForAssertion(() => Assert.Equal("Rename report", cut.Find("h2").TextContent));
    }

    [Fact]
    public void Render_ShouldPutTheOptionsOnTheDialog_WhenGivenThem()
    {
        var cut = Render<TbDialog>(parameters => parameters.Add(d => d.Open, true).Add(d => d.Title, "Rename")
            .Add(d => d.Options, new DialogOptions { Modal = false, Size = DialogSize.Large }));

        var dialog = cut.Find("dialog");
        Assert.Equal("manual", dialog.GetAttribute("popover"));
        Assert.Contains("tb-dialog--large", dialog.ClassList);
    }

    [Fact]
    public async Task Dispose_ShouldCloseTheDialog_WhenTheComponentGoesAway()
    {
        var cut = Render<TbDialog>(parameters => parameters.Add(d => d.Open, true).Add(d => d.Title, "Rename"));

        await DisposeComponentsAsync();

        Assert.Empty(Dialogs.OpenDialogs);
        cut.Dispose();
    }

    private sealed class CascadeReader : ComponentBase
    {
        [CascadingParameter]
        private string? Name { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) =>
            builder.AddContent(0, $"Hello {Name}");
    }
}
