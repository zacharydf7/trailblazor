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
    public async Task ConfirmAsync_ShouldKeepEveryDialog_WhenDialogsAreOpenedFromManyThreadsAtOnce()
    {
        // Resolved before the threads start, since the first resolution builds the test's service provider.
        var dialogs = Dialogs;
        await Task.WhenAll(Enumerable.Range(0, 8).Select(thread => Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
            {
                _ = dialogs.ConfirmAsync("Delete trail", "Delete it?");
            }
        })));

        Assert.Equal(4000, dialogs.OpenDialogs.Distinct().Count());
    }

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
    public void ShowAsync_ShouldMarkTheDialogWithItsPosition_WhenGivenOne()
    {
        var host = Render<DialogHost>();

        _ = Dialogs.ShowAsync<GreetingDialog, string>("Greeting",
            options: new() { Position = DialogPosition.BottomEnd });

        Assert.Contains("tb-dialog--position-bottomend", host.WaitForElement("dialog").ClassList);
    }

    [Fact]
    public void ShowAsync_ShouldTakeTheAppWideDismissalSettings_WhenItsOptionsLeaveThemUnset()
    {
        Services.Configure<TrailblazorOptions>(options => options.Dialogs.DismissOnBackdrop = false);

        _ = Dialogs.ShowAsync<GreetingDialog, string>("Greeting", options: new() { Size = DialogSize.Large });
        _ = Dialogs.ConfirmAsync("Delete?", "This can't be undone.");

        Assert.All(Dialogs.OpenDialogs, dialog => Assert.Equal((false, true),
            (dialog.Options.DismissOnBackdrop, dialog.Options.DismissOnEscape)));
    }

    [Fact]
    public void ShowAsync_ShouldKeepItsOwnDismissalOptions_WhenTheyDifferFromTheAppWideSettings()
    {
        Services.Configure<TrailblazorOptions>(options => options.Dialogs.DismissOnEscape = false);

        _ = Dialogs.ShowAsync<GreetingDialog, string>("Greeting",
            options: new() { DismissOnEscape = true, DismissOnBackdrop = false });

        var options = Dialogs.OpenDialogs.Single().Options;
        Assert.Equal((false, true), (options.DismissOnBackdrop, options.DismissOnEscape));
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
        var title = host.Find($"#{dialog.GetAttribute("aria-labelledby")}");
        Assert.Equal("Greeting", title.TextContent);
        Assert.Contains("tb-visually-hidden", title.ClassList);
        Assert.Empty(host.FindAll("header"));
    }

    [Fact]
    public async Task ShowHeader_ShouldHideAndRestoreTheHeader_WhenTheDialogChangesItWhileOpen()
    {
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<HeaderDialog, string>("Settings");

        await host.WaitForElement("[data-testid=toggle-header]").ClickAsync(new());
        host.WaitForAssertion(() => Assert.Empty(host.FindAll("header")));
        Assert.Equal("Settings", host.Find($"#{host.Find("dialog").GetAttribute("aria-labelledby")}").TextContent);
        await host.Find("[data-testid=toggle-header]").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Equal("Settings", host.Find("header h2").TextContent));
    }

    [Fact]
    public async Task CloseButton_ShouldGoAway_WhenTheDialogTurnsItOffWhileOpen()
    {
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<HeaderDialog, string>("Settings");

        await host.WaitForElement("[data-testid=toggle-close]").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Empty(host.FindAll("[data-part=close]")));
        Assert.Single(host.FindAll("header"));
    }

    [Fact]
    public async Task WindowButtons_ShouldTurnIntoRestore_WhenTheDialogIsMinimizedOrMaximized()
    {
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes",
            options: new() { Modal = false, Minimizable = true, Maximizable = true });
        var minimize = host.WaitForElement("[data-part=minimize]");
        Assert.Equal(("Minimize", "Maximize"), Labels(host));

        await minimize.ClickAsync(new());
        host.WaitForAssertion(() => Assert.Equal(("Restore", "Maximize"), Labels(host)));
        await host.Find("[data-part=maximize]").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Equal(("Minimize", "Restore"), Labels(host)));
    }

    [Fact]
    public async Task ShowHeader_ShouldRestoreAMinimizedDialog_WhenTheDialogHidesItsHeader()
    {
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<HeaderDialog, string>("Settings", options: new() { Modal = false, Minimizable = true });
        await host.WaitForElement("[data-part=minimize]").ClickAsync(new());

        await host.Find("[data-testid=toggle-header]").ClickAsync(new());

        Assert.Equal(DialogWindowState.Normal, Dialogs.OpenDialogs.Single().WindowState);
    }

    [Fact]
    public void ShowAsync_ShouldOfferAMoveButtonAndAResizeGripWithHints_WhenANonModalDialogIsDraggableAndResizable()
    {
        var host = Render<DialogHost>();

        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes",
            options: new() { Modal = false, Draggable = true, Resizable = true });

        var move = host.WaitForElement("[data-part=move] [data-part=trigger]");
        var grip = host.Find("[data-part=resize] [data-part=trigger]");
        Assert.Equal(("Move", "Resize"), (move.GetAttribute("aria-label"), grip.GetAttribute("aria-label")));
        Assert.Equal(("menu", "menu"), (move.GetAttribute("aria-haspopup"), grip.GetAttribute("aria-haspopup")));
        Assert.Equal("Arrow keys move the dialog.", host.Find($"#{move.GetAttribute("aria-describedby")}").TextContent);
        Assert.Equal("Arrow keys resize the dialog.",
            host.Find($"#{grip.GetAttribute("aria-describedby")}").TextContent);
        Assert.Contains("tb-dialog--draggable", host.Find("dialog").ClassList);
        Assert.Contains("tb-dialog--resizable", host.Find("dialog").ClassList);
    }

    [Fact]
    public void Menus_ShouldOfferEveryPlaceAndEverySizeButFull_WhenTheDialogIsDraggableAndResizable()
    {
        var host = Render<DialogHost>();

        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes",
            options: new() { Modal = false, Draggable = true, Resizable = true });

        host.WaitForElement("[data-part=move]");
        Assert.Equal(["Center", "Top", "Bottom", "Start", "End", "Top start", "Top end", "Bottom start", "Bottom end"],
            host.FindAll("[data-part=move] [role=menuitem]").Select(item => item.TextContent.Trim()));
        Assert.Equal(["Small", "Medium", "Large", "Extra large"],
            host.FindAll("[data-part=resize] [role=menuitem]").Select(item => item.TextContent.Trim()));
    }

    [Fact]
    public async Task MenuItems_ShouldTellTheBrowserDialogItsNewPlaceAndSize_WhenChosen()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        var handle = module.SetupModule("openDialog", _ => true);
        handle.SetupVoid("moveTo", _ => true).SetVoidResult();
        handle.SetupVoid("resizeTo", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes",
            options: new() { Modal = false, Draggable = true, Resizable = true, Size = DialogSize.Small });
        host.WaitForElement("[data-part=move]");

        await MenuItem(host, "move", "Top end").ClickAsync(new());
        await MenuItem(host, "resize", "Large").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Equal(["tb-dialog--position-center", "tb-dialog--position-topend"],
            handle.VerifyInvoke("moveTo").Arguments));
        host.WaitForAssertion(() => Assert.Equal(["tb-dialog--small", "tb-dialog--large"],
            handle.VerifyInvoke("resizeTo").Arguments));
    }

    [Fact]
    public async Task MoveMenu_ShouldStillTellTheBrowserDialog_WhenTheCurrentPlaceIsChosen()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        var handle = module.SetupModule("openDialog", _ => true);
        handle.SetupVoid("moveTo", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes", options: new() { Modal = false, Draggable = true });
        host.WaitForElement("[data-part=move]");

        await MenuItem(host, "move", "Center").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Equal(["tb-dialog--position-center", "tb-dialog--position-center"],
            handle.VerifyInvoke("moveTo").Arguments));
    }

    [Fact]
    public void ShowAsync_ShouldOfferNeitherMoveNorResize_WhenTheDialogIsModal()
    {
        var host = Render<DialogHost>();

        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes", options: new() { Draggable = true, Resizable = true });

        var dialog = host.WaitForElement("dialog");
        Assert.Empty(host.FindAll("[data-part=move], [data-part=resize]"));
        Assert.DoesNotContain("tb-dialog--draggable", dialog.ClassList);
        Assert.DoesNotContain("tb-dialog--resizable", dialog.ClassList);
    }

    [Fact]
    public async Task MoveButton_ShouldGoAway_WhenTheDialogIsMinimized()
    {
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes",
            options: new() { Modal = false, Draggable = true, Minimizable = true });
        host.WaitForElement("[data-part=move]");

        await host.Find("[data-part=minimize]").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Empty(host.FindAll("[data-part=move]")));
    }

    [Fact]
    public async Task Minimize_ShouldTellTheBrowserDialog_WhenClicked()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        var handle = module.SetupModule("openDialog", _ => true);
        handle.SetupVoid("setState", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes", options: new() { Modal = false, Minimizable = true });

        await host.WaitForElement("[data-part=minimize]").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Equal(["minimized", false], handle.VerifyInvoke("setState").Arguments));
    }

    [Fact]
    public async Task Minimize_ShouldLeaveThePageUsableUntilRestored_WhenTheDialogIsModal()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        module.SetupVoid("nextFrame", _ => true).SetVoidResult();
        var handle = module.SetupModule("openDialog", _ => true);
        handle.SetupVoid("setState", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes", options: new() { Minimizable = true });
        var dialog = Dialogs.OpenDialogs.Single();

        await host.WaitForElement("[data-part=minimize]").ClickAsync(new());
        host.WaitForAssertion(() => Assert.Single(handle.Invocations));
        var minimized = Services.GetRequiredService<DialogService>().TopmostModal;
        await host.Find("[data-part=minimize]").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Equal([["minimized", false], ["normal", true]],
            handle.Invocations.Select(call => call.Arguments)));
        Assert.Null(minimized);
        Assert.Same(dialog, Services.GetRequiredService<DialogService>().TopmostModal);
        Assert.True(dialog.Modal);
    }

    [Fact]
    public async Task Modal_ShouldSwitchTheOpenDialogAndMoveToastsIntoIt_WhenSetFromTheReference()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        var handle = module.SetupModule("openDialog", _ => true);
        handle.SetupVoid("setState", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes", options: new() { Modal = false, Draggable = true });
        host.WaitForElement("[data-part=move]");

        await host.InvokeAsync(() => Dialogs.OpenDialogs.Single().Modal = true);

        host.WaitForAssertion(() => Assert.Equal(["normal", true], handle.VerifyInvoke("setState").Arguments));
        Assert.Empty(host.FindAll("[data-part=move]"));
        Assert.DoesNotContain("tb-dialog--draggable", host.Find("dialog").ClassList);
        Assert.Single(host.FindAll("dialog [data-part=toaster]"));
    }

    [Fact]
    public async Task SizeAndPosition_ShouldResizeAndMoveTheOpenDialog_WhenSetFromTheReference()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        module.SetupVoid("nextFrame", _ => true).SetVoidResult();
        var handle = module.SetupModule("openDialog", _ => true);
        handle.SetupVoid("moveTo", _ => true).SetVoidResult();
        handle.SetupVoid("resizeTo", _ => true).SetVoidResult();
        handle.SetupVoid("setState", _ => true).SetVoidResult();
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes");
        host.WaitForElement("dialog");
        var dialog = Dialogs.OpenDialogs.Single();

        await host.InvokeAsync(() =>
        {
            dialog.Size = DialogSize.Full;
            dialog.Position = DialogPosition.Top;
        });

        host.WaitForAssertion(() => Assert.Equal(["tb-dialog--medium", "tb-dialog--full"],
            handle.VerifyInvoke("resizeTo").Arguments));
        host.WaitForAssertion(() => Assert.Equal(["tb-dialog--position-center", "tb-dialog--position-top"],
            handle.VerifyInvoke("moveTo").Arguments));
        // A later render, here for turning modeless, keeps the new size and place.
        await host.InvokeAsync(() => dialog.Modal = false);
        Assert.Contains("tb-dialog--full", host.Find("dialog").ClassList);
        Assert.Contains("tb-dialog--position-top", host.Find("dialog").ClassList);
    }

    [Fact]
    public void Size_ShouldShowAtOnce_WhenTheContentSetsItAsItStarts()
    {
        var host = Render<DialogHost>();

        _ = Dialogs.ShowAsync<SmallDialog, string>("Notes");

        host.WaitForAssertion(() => Assert.Contains("tb-dialog--small", host.Find("dialog").ClassList));
    }

    [Fact]
    public async Task ShowDynamic_ShouldOpenTheContentTypeWithItsParametersByName_WhenGivenAType()
    {
        var host = Render<DialogHost>();
        Type chosen = typeof(GreetingDialog);

        var result = Dialogs.ShowDynamicAsync<string>(chosen, "Greeting",
            new Dictionary<string, object?> { ["name"] = "Ada" });

        Assert.Contains("Hello Ada", host.WaitForElement("dialog").TextContent);
        await host.Find("[data-testid=accept]").ClickAsync(new());
        Assert.Equal("Ada", (await result).Value);
    }

    [Fact]
    public void ShowDynamic_ShouldRefuseATypeThatIsntContentForTheResult_WhenGivenOne()
    {
        var notContent = Assert.Throws<ArgumentException>(() =>
            Dialogs.ShowDynamic<string>(typeof(string), "Greeting"));
        var otherResult = Assert.Throws<ArgumentException>(() =>
            Dialogs.ShowDynamic<bool>(typeof(GreetingDialog), "Greeting"));

        var abstractContent = Assert.Throws<ArgumentException>(() =>
            Dialogs.ShowDynamic<string>(typeof(TbDialogContent<string>), "Greeting"));

        Assert.Equal("dialog", notContent.ParamName);
        Assert.Equal("dialog", otherResult.ParamName);
        Assert.Equal("dialog", abstractContent.ParamName);
        Assert.Empty(Dialogs.OpenDialogs);
    }

    [Fact]
    public void ShowDynamic_ShouldRefuseAValueOfTheWrongType_WhenGivenOne()
    {
        var error = Assert.Throws<ArgumentException>(() => Dialogs.ShowDynamic<string>(typeof(GreetingDialog),
            "Greeting", new Dictionary<string, object?> { ["Name"] = 42 }));
        var empty = Dialogs.ShowDynamic<string>(typeof(GreetingDialog), "Greeting",
            new Dictionary<string, object?> { ["Name"] = null });

        var missing = Assert.Throws<ArgumentException>(() => Dialogs.ShowDynamic<string>(typeof(SmallDialog),
            "Notes", new Dictionary<string, object?> { ["Lines"] = null }));

        Assert.Equal("parameters", error.ParamName);
        Assert.Contains("String", error.Message);
        Assert.Contains("Int32", missing.Message);
        Assert.Same(empty, Assert.Single(Dialogs.OpenDialogs));
    }

    [Fact]
    public void ShowDynamic_ShouldRefuseANameThatIsntAParameter_WhenGivenOne()
    {
        var error = Assert.Throws<ArgumentException>(() => Dialogs.ShowDynamic<string>(typeof(GreetingDialog),
            "Greeting", new Dictionary<string, object?> { ["Nickname"] = "Ada" }));

        Assert.Equal("parameters", error.ParamName);
        Assert.Contains("Nickname", error.Message);
        Assert.Empty(Dialogs.OpenDialogs);
    }

    [Fact]
    public void WindowState_ShouldRefuseToMinimize_WhenTheDialogIsNotMinimizable()
    {
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Notes", options: new() { Modal = false });

        Assert.Throws<InvalidOperationException>(() =>
            Dialogs.OpenDialogs.Single().WindowState = DialogWindowState.Minimized);
    }

    [Fact]
    public async Task WindowState_ShouldMaximizeTheDialog_WhenItsContentSetsIt()
    {
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<LayoutDialog, string>("Notes", options: new() { Maximizable = true });

        await host.WaitForElement("[data-testid=maximize]").ClickAsync(new());

        Assert.Equal(DialogWindowState.Maximized, Dialogs.OpenDialogs.Single().WindowState);
        Assert.Equal("Restore", host.Find("[data-part=maximize]").GetAttribute("aria-label"));
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
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        module.SetupVoid("nextFrame").SetVoidResult();
        var handle = module.SetupModule("openDialog", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        var host = Render<DialogHost>();
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Greeting");
        host.WaitForElement("dialog");

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }

    [Fact]
    public async Task CloseAsync_ShouldStopCountingTheDialogAsTheTopmostModal_WhenItStartsToFadeOut()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        module.SetupVoid("nextFrame").SetVoidResult();
        var close = module.SetupModule("openDialog", _ => true).SetupVoid("close");
        var host = Render<DialogHost>();
        var changes = 0;
        _ = Dialogs.ShowAsync<GreetingDialog, string>("Greeting", p => p.Set(d => d.Name, "Ada"));
        var service = Services.GetRequiredService<DialogService>();
        var accept = host.WaitForElement("[data-testid=accept]");
        Assert.Same(Dialogs.OpenDialogs.Single(), service.TopmostModal);
        service.Changed += () => changes++;

        var closing = accept.ClickAsync(new());

        Assert.Null(service.TopmostModal);
        Assert.Equal(1, changes);
        close.SetVoidResult();
        await closing;
    }

    [Fact]
    public async Task CloseAsync_ShouldStillCompleteAndRemoveTheDialog_WhenTheBrowserCloseFails()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        module.SetupVoid("nextFrame").SetVoidResult();
        var handle = module.SetupModule("openDialog", _ => true);
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

    [Fact]
    public async Task Show_ShouldReturnAReferenceThatClosesTheDialogWithAValue_WhenTheCallerCloses()
    {
        var host = Render<DialogHost>();
        var dialog = Dialogs.Show<GreetingDialog, string>("Greeting");
        host.WaitForElement("dialog");

        await host.InvokeAsync(() => dialog.CloseAsync("Bo"));

        Assert.Equal(DialogResult<string>.Ok("Bo"), await dialog.Result);
        Assert.Empty(host.FindAll("dialog"));
    }

    [Fact]
    public void Title_ShouldRenameTheOpenDialog_WhenTheCallerSetsIt()
    {
        var host = Render<DialogHost>();
        var dialog = Dialogs.Show<GreetingDialog, string>("Greeting");
        host.WaitForElement("dialog");

        dialog.Title = "Welcome";

        host.WaitForAssertion(() => Assert.Equal("Welcome", host.Find("h2").TextContent));
    }

    [Fact]
    public void Update_ShouldRenderTheContentWithTheNewParameters_WhenTheCallerUpdatesIt()
    {
        var host = Render<DialogHost>();
        var dialog = Dialogs.Show<GreetingDialog, string>("Greeting", p => p.Set(d => d.Name, "Ada"));
        host.WaitForElement("dialog");

        dialog.Update<GreetingDialog>(p => p.Set(d => d.Name, "Bo"));

        host.WaitForAssertion(() => Assert.Contains("Hello Bo", host.Find("[data-part=body]").TextContent));
        Assert.Throws<InvalidOperationException>(() => dialog.Update<HeaderDialog>(_ => { }));
    }

    [Fact]
    public async Task Update_ShouldKeepAParameterTheDialogChanged_WhenTheUpdateNamesAnother()
    {
        var host = Render<DialogHost>();
        var dialog = Dialogs.Show<EditableDialog, bool>("Edit", p => p.Set(d => d.Name, "Ada").Set(d => d.Note, "new"));
        await host.WaitForElement("[data-testid=type]").ClickAsync(new());

        dialog.Update<EditableDialog>(p => p.Set(d => d.Note, "taken"));

        host.WaitForAssertion(() => Assert.Equal("Typed / taken", host.Find("[data-part=body]").TextContent.Trim()));
    }

    [Fact]
    public async Task CloseAllAsync_ShouldCancelEveryOpenDialog_WhenCalled()
    {
        var host = Render<DialogHost>();
        var first = Dialogs.Show<GreetingDialog, string>("First");
        var second = Dialogs.Show<GreetingDialog, string>("Second");
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("dialog").Count));

        await host.InvokeAsync(Dialogs.CloseAllAsync);

        Assert.Equal([DialogResult<string>.Cancel(), DialogResult<string>.Cancel()],
            await Task.WhenAll(first.Result, second.Result));
        Assert.Empty(Dialogs.OpenDialogs);
    }

    [Fact]
    public async Task Dismiss_ShouldAskTheContentWithTheReasonAndStayOpen_WhenTheContentRefuses()
    {
        var host = Render<DialogHost>();
        var dialog = Dialogs.Show<GuardedDialog, bool>("Edit");
        var content = host.WaitForComponent<GuardedDialog>();

        await host.Find("[data-part=close]").ClickAsync(new());
        await host.InvokeAsync(() => host.FindComponent<DialogFrame>().Instance.Dismiss(DialogDismissReason.Escape));
        await host.InvokeAsync(() => host.FindComponent<DialogFrame>().Instance.Dismiss(DialogDismissReason.Backdrop));

        Assert.Equal([DialogDismissReason.CloseButton, DialogDismissReason.Escape, DialogDismissReason.Backdrop],
            content.Instance.Asked);
        Assert.False(dialog.Result.IsCompleted);
        content.Instance.Allow = true;
        await host.Find("[data-part=close]").ClickAsync(new());
        Assert.Equal(DialogResult<bool>.Cancel(), await dialog.Result);
    }

    [Fact]
    public async Task CancelAsync_ShouldCloseWithoutAskingTheContent_WhenTheCallerCancels()
    {
        var host = Render<DialogHost>();
        var dialog = Dialogs.Show<GuardedDialog, bool>("Edit");
        var asked = host.WaitForComponent<GuardedDialog>().Instance.Asked;

        await host.InvokeAsync(dialog.CancelAsync);

        Assert.Equal(DialogResult<bool>.Cancel(), await dialog.Result);
        Assert.Empty(asked);
    }

    [Fact]
    public async Task Dismiss_ShouldReportAFailingQuestionToTheRenderer_WhenTheContentThrows()
    {
        var host = Render<DialogHost>();
        var dialog = Dialogs.Show<GuardedDialog, bool>("Edit");
        host.WaitForComponent<GuardedDialog>().Instance.Failure = new InvalidOperationException("Saving failed.");

        await host.InvokeAsync(() => host.FindComponent<DialogFrame>().Instance.Dismiss(DialogDismissReason.Escape));

        var exception = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5),
            Xunit.TestContext.Current.CancellationToken);
        Assert.Equal("Saving failed.", exception.Message);
        Assert.False(dialog.Result.IsCompleted);
    }

    [Fact]
    public async Task AlertAsync_ShouldShowOneButtonAndTheTonesIcon_WhenCalled()
    {
        var host = Render<DialogHost>();

        var alerting = Dialogs.AlertAsync("Saved", "The report is saved.", tone: Tone.Success);

        var dialog = host.WaitForElement("dialog");
        Assert.Equal("alertdialog", dialog.GetAttribute("role"));
        Assert.Equal("The report is saved.", host.Find($"#{dialog.GetAttribute("aria-describedby")}").TextContent);
        Assert.Contains("tb-icon--success", host.Find("[data-part=body] [data-part=icon]").ClassList);
        var button = Assert.Single(host.FindAll("[data-part=actions] button"));
        Assert.Equal("OK", button.TextContent.Trim());
        await button.ClickAsync(new());
        await alerting;
    }

    [Fact]
    public void Description_ShouldDescribeTheDialog_WhenItsContentHasOne()
    {
        var host = Render<DialogHost>();

        _ = Dialogs.Show<DescribedDialog, bool>("Export");

        var description = host.WaitForElement("[data-part=description]");
        host.WaitForAssertion(() => Assert.Equal(description.Id, host.Find("dialog").GetAttribute("aria-describedby")));
        Assert.Equal("Exports every page as a PDF.", description.TextContent.Trim());
        Assert.Contains("wide", description.ClassList);
        Assert.Equal("color: red", description.GetAttribute("style"));
        Assert.Equal("intro", description.GetAttribute("data-testid"));
    }

    [Fact]
    public void Description_ShouldLeaveTheDialogUndescribed_WhenItsContentHasNone()
    {
        var host = Render<DialogHost>();

        _ = Dialogs.Show<GreetingDialog, string>("Greeting");

        Assert.Null(host.WaitForElement("dialog").GetAttribute("aria-describedby"));
    }

    private sealed class GuardedDialog : TbDialogContent<bool>
    {
        public List<DialogDismissReason> Asked { get; } = [];

        public bool Allow { get; set; }

        public Exception? Failure { get; set; }

        protected override Task<bool> CanDismissAsync(DialogDismissReason reason)
        {
            Asked.Add(reason);
            return Failure is not null ? Task.FromException<bool>(Failure) : Task.FromResult(Allow);
        }
    }

    private sealed class EditableDialog : TbDialogContent<bool>
    {
        [Parameter]
        public string? Name { get; set; }

        [Parameter]
        public string? Note { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"{Name} / {Note}");
            builder.OpenElement(1, "button");
            builder.AddAttribute(2, "data-testid", "type");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => Name = "Typed"));
            builder.CloseElement();
        }
    }

    private sealed class DescribedDialog : TbDialogContent<bool>
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbDialogDescription>(0);
            builder.AddComponentParameter(1, nameof(TbDialogDescription.Class), "wide");
            builder.AddComponentParameter(2, nameof(TbDialogDescription.Style), "color: red");
            builder.AddAttribute(3, "data-testid", "intro");
            builder.AddComponentParameter(4, nameof(TbDialogDescription.ChildContent),
                (RenderFragment)(text => text.AddContent(0, "Exports every page as a PDF.")));
            builder.CloseComponent();
        }
    }

    private sealed class Settings
    {
        public string Theme { get; set; } = "light";
    }

    private sealed class CountingDialog : TbDialogContent<bool>
    {
        [Parameter]
        public Settings? Settings { get; set; }

        public int ParametersSet { get; private set; }

        protected override void OnParametersSet() => ParametersSet++;
    }

    private static AngleSharp.Dom.IElement MenuItem(IRenderedComponent<DialogHost> host, string menu, string text) =>
        host.FindAll($"[data-part={menu}] [role=menuitem]").Single(item => item.TextContent.Trim() == text);

    private static (string?, string?) Labels(IRenderedComponent<DialogHost> host) =>
        (host.Find("[data-part=minimize]").GetAttribute("aria-label"),
            host.Find("[data-part=maximize]").GetAttribute("aria-label"));

    private sealed class HeaderDialog : TbDialogContent<string>
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "data-testid", "toggle-header");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => ShowHeader = !ShowHeader));
            builder.CloseElement();
            builder.OpenElement(3, "button");
            builder.AddAttribute(4, "data-testid", "toggle-close");
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => CloseButton = !CloseButton));
            builder.CloseElement();
        }
    }

    private sealed class SmallDialog : TbDialogContent<string>
    {
        [Parameter]
        public int Lines { get; set; }

        protected override void OnInitialized() => Size = DialogSize.Small;
    }

    private sealed class LayoutDialog : TbDialogContent<string>
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "data-testid", "maximize");
            builder.AddAttribute(2, "onclick",
                EventCallback.Factory.Create(this, () => WindowState = DialogWindowState.Maximized));
            builder.CloseElement();
        }
    }

    private sealed class GreetingDialog : TbDialogContent<string>
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
