using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;

namespace Trailblazor.Tests;

public class TrailblazorBunitExtensionsTests : BunitContext
{
    private string? _size;

    [Fact]
    public async Task AddTrailblazor_ShouldLetScriptBackedComponentsRenderAndRespond_WhenNoJsInteropIsSetUp()
    {
        this.AddTrailblazor();
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbRoot>(0);
            builder.CloseComponent();
            builder.OpenComponent<TbTooltip>(1);
            builder.AddAttribute(2, nameof(TbTooltip.Text), "Saves the file");
            builder.AddAttribute(3, nameof(TbTooltip.ChildContent),
                (RenderFragment)(button => button.AddContent(0, "Save")));
            builder.CloseComponent();
            builder.OpenComponent<TbMenu>(4);
            builder.AddAttribute(5, nameof(TbMenu.Label), "More");
            builder.CloseComponent();
            builder.OpenComponent<TbSelect<string>>(6);
            builder.AddAttribute(7, nameof(TbSelect<string>.Label), "Size");
            builder.AddAttribute(8, nameof(TbSelect<string>.ValueChanged),
                EventCallback.Factory.Create<string>(this, size => _size = size));
            builder.AddAttribute(9, nameof(TbSelect<string>.ChildContent), (RenderFragment)AddSizes);
            builder.CloseComponent();
            builder.OpenComponent<TbThemePicker>(10);
            builder.CloseComponent();
        });

        cut.Find("[role=option]").Click();
        Services.GetRequiredService<IToastService>().Show("Saved");
        _ = Services.GetRequiredService<IDialogService>().ConfirmAsync("Delete?", "This can't be undone.");

        Assert.Equal("Small", _size);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-part=toast]")));
        await cut.WaitForAssertionAsync(() => Assert.Single(cut.FindAll("dialog")));
    }

    [Fact]
    public void AddTrailblazor_ShouldLeaveATestsOwnModuleSetupInCharge_WhenTheTestSetsOneUp()
    {
        this.AddTrailblazor();
        var select = JSInterop.SetupModule("./_content/Trailblazor/select.js");
        select.SetupModule("createSelect", _ => true);

        Render<TbSelect<string>>(parameters => parameters.Add(component => component.ChildContent, AddSizes));

        Assert.Single(select.Invocations["createSelect"]);
    }

    [Fact]
    public async Task AddTrailblazor_ShouldLeaveOtherModulesUnanswered_WhenTheTestHasNotSetThemUp()
    {
        this.AddTrailblazor();
        var js = Services.GetRequiredService<IJSRuntime>();

        await Assert.ThrowsAsync<JSRuntimeUnhandledInvocationException>(() => js
            .InvokeAsync<IJSObjectReference>("import", ["./_content/TrailblazorExtras/extras.js"]).AsTask());
    }

    [Fact]
    public void AddTrailblazor_ShouldRenderAsAnInteractivePage_WhenTheTestSetsNoRenderer()
    {
        this.AddTrailblazor();

        var cut = Render<TbSelect<string>>(parameters => parameters.Add(component => component.ChildContent, AddSizes));

        Assert.Single(cut.FindAll("[role=combobox]"));
        Assert.Empty(cut.FindAll("select"));
    }

    [Fact]
    public void AddTrailblazor_ShouldKeepTheTestsRenderer_WhenTheTestSetsAStaticOne()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbSelect<string>>(parameters => parameters.Add(component => component.ChildContent, AddSizes));

        Assert.Single(cut.FindAll("select"));
        Assert.Empty(cut.FindAll("[role=combobox]"));
    }

    [Fact]
    public void AddTrailblazor_ShouldStillTakeServices_WhenTheTestAddsThemAfterward()
    {
        this.AddTrailblazor();
        var time = new FakeTimeProvider();
        Services.AddSingleton<TimeProvider>(time);

        Render<TbSelect<string>>(parameters => parameters.Add(component => component.ChildContent, AddSizes));

        Assert.Same(time, Services.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void AddTrailblazor_ShouldApplyTheOptions_WhenConfigured()
    {
        this.AddTrailblazor(options => options.Toasts.MaxVisible = 1);

        Assert.Equal(1, Services.GetRequiredService<IOptions<TrailblazorOptions>>().Value.Toasts.MaxVisible);
    }

    [Fact]
    public async Task ShowDialogAsync_ShouldShowTheDialogWithItsTitleParametersAndOptions_WhenCalled()
    {
        this.AddTrailblazor();

        var dialog = await this.ShowDialogAsync<RenameDialog, string>("Rename trail",
            parameters => parameters.Set(rename => rename.Name, "Pine Hollow"), new() { Size = DialogSize.Small });

        var shown = dialog.Root.Find("dialog");
        Assert.Equal("Rename trail", dialog.Root.Find($"#{shown.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Contains("tb-dialog--small", shown.ClassList);
        Assert.Equal("Pine Hollow", dialog.Root.Find("input").GetAttribute("value"));
        Assert.Equal("Pine Hollow", dialog.Content.Instance.Name);
        Assert.Equal("Save", dialog.Root.Find("[data-testid=save]").TextContent);
        Assert.Same(dialog.Reference, Services.GetRequiredService<IDialogService>().OpenDialogs.Single());
    }

    [Fact]
    public async Task ShowDialogAsync_ShouldShowTheDialogOnce_WhenCalledOnARootTheTestRendered()
    {
        this.AddTrailblazor();
        var root = Render<TbRoot>();

        var dialog = await root.ShowDialogAsync<NoticeDialog>("Trail notice");

        Assert.Same(root, dialog.Root);
        Assert.Single(root.FindAll("dialog"));
    }

    [Fact]
    public async Task ShowDialogAsync_ShouldOpenADialogWithABooleanResult_WhenItIsATbDialogContentOfBool()
    {
        this.AddTrailblazor();

        var dialog = await this.ShowDialogAsync<KeepGoingDialog>("Keep going?");
        await dialog.Reference.CloseAsync(true);

        Assert.True((await dialog.Reference.Result).Value);
    }

    [Fact]
    public async Task RootTb_ShouldDriveTheDialogAPageOpens_WhenTheTestRendersTheRootBesideThePage()
    {
        this.AddTrailblazor();
        var root = Render<TbRoot>();
        var page = Render<DistancePage>();

        var click = page.Find("button").ClickAsync(new());
        await root.Tb().NumberField("Distance").EnterAsync("12");
        await root.Find("[data-testid=save]").ClickAsync(new());
        await click;

        Assert.Equal("12 km", page.Find("output").TextContent);
    }

    [Fact]
    public async Task ShowDialogAsync_ShouldThrow_WhenTheTestRendersARootOfItsOwnAfterTheFirstCall()
    {
        this.AddTrailblazor();
        await this.ShowDialogAsync<NoticeDialog>("Trail notice");
        Render<TbRoot>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => this.ShowDialogAsync<NoticeDialog>("Trail notice"));
    }

    [Fact]
    public async Task RootShowDialogAsync_ShouldThrow_WhenTheTestRenderedAnotherRoot()
    {
        this.AddTrailblazor();
        await this.ShowDialogAsync<NoticeDialog>("Trail notice");
        var root = Render<TbRoot>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => root.ShowDialogAsync<NoticeDialog>("Trail notice"));
    }

    [Fact]
    public async Task ShowDialogAsync_ShouldThrow_WhenTheTestRenderedARootOfItsOwn()
    {
        this.AddTrailblazor();
        Render<TbRoot>();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => this.ShowDialogAsync<NoticeDialog>("Trail notice"));

        Assert.Contains("ShowDialogAsync on it", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowDialogAsync_ShouldShowADialogWithNoResult_WhenGivenOnlyTheDialogType()
    {
        this.AddTrailblazor();

        var dialog = await this.ShowDialogAsync<NoticeDialog>("Trail notice");

        Assert.IsType<NoticeDialog>(dialog.Content.Instance);
        await dialog.Reference.CancelAsync();
        Assert.False((await dialog.Reference.Result).Confirmed);
    }

    [Fact]
    public async Task ShowDialogAsync_ShouldFinishTheResult_WhenTheDialogCloses()
    {
        this.AddTrailblazor();
        var dialog = await this.ShowDialogAsync<RenameDialog, string>("Rename trail",
            parameters => parameters.Set(rename => rename.Name, "Pine Hollow"));

        await dialog.Root.Find("[data-testid=save]").ClickAsync(new());

        var result = await dialog.Reference.Result;
        Assert.Equal("Pine Hollow", result.Value);
        Assert.Empty(dialog.Root.FindAll("dialog"));
    }

    [Fact]
    public async Task ShowDialogAsync_ShouldShowEachDialogInOneRootWithItsOwnContent_WhenCalledAgainInTheSameTest()
    {
        this.AddTrailblazor();
        var first = await this.ShowDialogAsync<RenameDialog, string>("Rename trail",
            parameters => parameters.Set(rename => rename.Name, "Pine Hollow"));

        var second = await this.ShowDialogAsync<RenameDialog, string>("Rename camp",
            parameters => parameters.Set(rename => rename.Name, "Lake camp"));

        Assert.Same(first.Root, second.Root);
        Assert.Equal("Pine Hollow", first.Content.Instance.Name);
        Assert.Equal("Lake camp", second.Content.Instance.Name);
    }

    [Fact]
    public async Task ShowDialogAsync_ShouldHostTheDialogAgain_WhenTheTestDisposedTheLastRoot()
    {
        this.AddTrailblazor();
        var first = await this.ShowDialogAsync<RenameDialog, string>("Rename trail");
        await DisposeComponentsAsync();

        var second = await this.ShowDialogAsync<RenameDialog, string>("Rename camp");

        Assert.NotSame(first.Root, second.Root);
        Assert.Contains("Rename camp", second.Root.Markup);
    }

    private sealed class NoticeDialog : TbDialogContent;

    private sealed class KeepGoingDialog : TbDialogContent<bool>;

    private sealed class DistancePage : ComponentBase
    {
        private string _shown = "";

        [Inject]
        private IDialogService Dialogs { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, async () =>
                _shown = $"{(await Dialogs.Show<DistanceDialog, int>("Distance").Result).Value} km"));
            builder.CloseElement();
            builder.OpenElement(2, "output");
            builder.AddContent(3, _shown);
            builder.CloseElement();
        }
    }

    private sealed class DistanceDialog : TbDialogContent<int>
    {
        private int _distance;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbNumberField<int>>(0);
            builder.AddAttribute(1, nameof(TbNumberField<int>.Label), "Distance");
            builder.AddAttribute(2, nameof(TbNumberField<int>.Value), _distance);
            builder.AddAttribute(3, nameof(TbNumberField<int>.ValueChanged),
                EventCallback.Factory.Create<int>(this, distance => _distance = distance));
            builder.CloseComponent();
            builder.OpenElement(4, "button");
            builder.AddAttribute(5, "data-testid", "save");
            builder.AddAttribute(6, "onclick", EventCallback.Factory.Create(this, () => Dialog.CloseAsync(_distance)));
            builder.CloseElement();
        }
    }

    // Content that reads how it renders, as fields and uploads do, finds itself interactive, as on a page.
    private sealed class RenameDialog : TbDialogContent<string>
    {
        [Parameter]
        public string Name { get; set; } = "";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbTextField<string>>(0);
            builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
            builder.AddAttribute(2, nameof(TbTextField<string>.Value), Name);
            builder.CloseComponent();
            builder.OpenElement(3, "button");
            builder.AddAttribute(4, "data-testid", "save");
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => Dialog.CloseAsync(Name)));
            builder.AddContent(6, RendererInfo.IsInteractive ? "Save" : "Not interactive");
            builder.CloseElement();
        }
    }

    private static void AddSizes(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbOption<string>>(0);
        builder.AddAttribute(1, nameof(TbOption<string>.Value), "Small");
        builder.CloseComponent();
    }
}
