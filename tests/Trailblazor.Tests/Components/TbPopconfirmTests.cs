using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbPopconfirmTests : BunitContext
{
    public TbPopconfirmTests()
    {
        this.AddTrailblazor();
    }

    private List<string> Events { get; } = [];

    private Task Confirmed()
    {
        Events.Add("confirmed");
        return Task.CompletedTask;
    }

    private IRenderedComponent<TbPopconfirm> RenderConfirm(Func<Task>? onConfirm = null,
        Action<ComponentParameterCollectionBuilder<TbPopconfirm>>? configure = null) =>
        Render<TbPopconfirm>(parameters =>
    {
        parameters.Add(confirm => confirm.Label, "Delete").Add(confirm => confirm.Title, "Delete Pine Hollow loop?")
            .Add(confirm => confirm.OnConfirm, onConfirm ?? Confirmed)
            .Add(confirm => confirm.OnCancel, () => Events.Add("canceled"))
            .Add(confirm => confirm.OpenChanged, (bool open) => Events.Add(open ? "opened" : "closed"));
        configure?.Invoke(parameters);
    });

    [Fact]
    public void Render_ShouldNameThePanelByItsQuestionAndPutCancelFirst_WhenCalled()
    {
        var cut = RenderConfirm(configure: parameters => parameters
            .Add(confirm => confirm.Description, "This can't be undone.").Add(confirm => confirm.ConfirmText, "Delete")
            .Add(confirm => confirm.Tone, Tone.Danger));

        var panel = cut.Find("[data-part=panel]");
        var title = cut.Find($"[id='{panel.GetAttribute("aria-labelledby")}']");
        Assert.Equal("Delete Pine Hollow loop?", title.TextContent);
        Assert.Equal("This can't be undone.", cut.Find("[data-part=description]").TextContent.Trim());
        Assert.Equal(["Cancel", "Delete"], cut.FindAll("[data-part=actions] button").Select(button =>
            button.TextContent.Trim()));
        Assert.Contains("tb-button--danger", cut.Find("[data-part=confirm]").ClassList);
    }

    [Fact]
    public void Render_ShouldShowItsOwnContent_WhenGivenChildContent()
    {
        var cut = RenderConfirm(configure: parameters => parameters.Add(confirm => confirm.Description, "Unused")
            .AddChildContent("<strong>3 photos</strong> go with it."));

        Assert.Equal("<strong>3 photos</strong> go with it.", cut.Find("[data-part=description]").InnerHtml.Trim());
        Assert.Equal("OK", cut.Find("[data-part=confirm]").TextContent.Trim());
    }

    [Fact]
    public async Task Confirm_ShouldBeBusyUntilOnConfirmFinishesAndThenClose_WhenClicked()
    {
        var save = new TaskCompletionSource();
        var cut = RenderConfirm(() => save.Task, parameters => parameters.Add(confirm => confirm.Open, true));

        var confirming = cut.Find("[data-part=confirm]").ClickAsync(new());
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-part=confirm]").GetAttribute("aria-busy")));
        Assert.True(cut.Find("[data-part=cancel]").HasAttribute("disabled"));
        Assert.Empty(Events);

        save.SetResult();
        await confirming;

        Assert.Equal(["closed"], Events);
    }

    [Fact]
    public async Task Cancel_ShouldCloseAndRaiseOnCancel_WhenClicked()
    {
        var cut = RenderConfirm(configure: parameters => parameters.Add(confirm => confirm.Open, true));

        await cut.Find("[data-part=cancel]").ClickAsync(new());

        Assert.Equal(["closed", "canceled"], Events);
    }

    [Fact]
    public async Task Dismiss_ShouldCountAsACancel_WhenThePopoverClosesWithoutAnAnswer()
    {
        var cut = RenderConfirm();
        var popover = cut.FindComponent<TbPopover>().Instance;

        await cut.InvokeAsync(() => popover.OnToggleAsync(true));
        await cut.InvokeAsync(() => popover.OnToggleAsync(false));

        Assert.Equal(["opened", "closed", "canceled"], Events);
    }

    [Fact]
    public void Render_ShouldOpenThePopover_WhenTheParentSetsOpen()
    {
        var cut = RenderConfirm(configure: parameters => parameters.Add(confirm => confirm.Open, true));

        Assert.True(cut.FindComponent<TbPopover>().Instance.Open);
    }

    [Fact]
    public async Task Confirm_ShouldStayOpenAndBusy_WhenAParentThatDoesntBindOpenRendersAgain()
    {
        var cut = Render<UnboundHost>();
        await cut.InvokeAsync(() => cut.FindComponent<TbPopover>().Instance.OnToggleAsync(true));

        var confirming = cut.Find("[data-part=confirm]").ClickAsync(new());

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-part=confirm]").GetAttribute("aria-busy")));
        Assert.True(cut.Instance.Renders > 1);
        Assert.True(cut.FindComponent<TbPopover>().Instance.Open);
        cut.Instance.Save.SetResult();
        await confirming;
        Assert.False(cut.FindComponent<TbPopover>().Instance.Open);
    }

    [Fact]
    public void Render_ShouldDescribeThePanelByItsDescription_WhenItHasOne()
    {
        var described = RenderConfirm(configure: parameters => parameters.Add(confirm => confirm.Description,
            "This can't be undone."));
        var plain = RenderConfirm();

        Assert.Equal(described.Find("[data-part=description]").Id,
            described.Find("[data-part=panel]").GetAttribute("aria-describedby"));
        Assert.Null(plain.Find("[data-part=panel]").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Render_ShouldLeaveAPopoverInsideUndescribed_WhenItsContentHoldsOne()
    {
        var cut = RenderConfirm(configure: parameters => parameters.AddChildContent<TbPopover>(inner => inner
            .Add(popover => popover.Label, "Why?").AddChildContent("Photos can't be restored.")));

        var panels = cut.FindAll("[data-part=panel]");
        Assert.Equal(cut.Find("[data-part=description]").Id, panels[0].GetAttribute("aria-describedby"));
        Assert.Null(panels[1].GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Render_ShouldTakeTheGroupsLook_WhenItsTriggerIsInAButtonGroup()
    {
        var cut = Render<TbButtonGroup>(parameters => parameters.Add(group => group.Variant, Variant.Filled)
            .Add(group => group.Size, Size.Small).AddChildContent<TbPopconfirm>(confirm => confirm
                .Add(item => item.Label, "Delete").Add(item => item.Title, "Delete Pine Hollow loop?")));

        var trigger = cut.Find("[data-part=trigger]");
        Assert.Contains("tb-button--filled", trigger.ClassList);
        Assert.Contains("tb-button--small", trigger.ClassList);
    }

    [Fact]
    public void Render_ShouldNameItselfInTheError_WhenAnIconTriggerHasNoAriaLabel()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbPopconfirm>(parameters => parameters
            .Add(confirm => confirm.Label, "Delete")
            .Add(confirm => confirm.Icon, new Icon("<path d=\"M4 4h16v16H4z\" />"))
            .Add(confirm => confirm.Title, "Delete Pine Hollow loop?")));

        Assert.StartsWith("TbPopconfirm with an Icon needs an AriaLabel", error.Message);
    }

    [Fact]
    public void Render_ShouldNameItselfInTheError_WhenItHasNoLabelOrTrigger()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbPopconfirm>(parameters => parameters
            .Add(confirm => confirm.Title, "Delete Pine Hollow loop?")));

        Assert.Equal("TbPopconfirm needs a Label or a Trigger.", error.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheTitleIsBlank()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbPopconfirm>(parameters => parameters
            .Add(confirm => confirm.Label, "Delete").Add(confirm => confirm.Title, " ")));
    }

    // A page that doesn't bind Open. Blazor renders it again as soon as its OnConfirm starts, as it does for any
    // handler.
    private sealed class UnboundHost : ComponentBase
    {
        private static readonly Icon Archive = new("<path d=\"M4 4h16v16H4z\" />");

        public TaskCompletionSource Save { get; } = new();

        public int Renders { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Renders++;
            builder.OpenComponent<TbPopconfirm>(0);
            builder.AddComponentParameter(1, nameof(TbPopconfirm.AriaLabel), "Archive");
            builder.AddComponentParameter(2, nameof(TbPopconfirm.Title), "Archive Cedar Flats?");
            builder.AddComponentParameter(3, nameof(TbPopconfirm.OnConfirm),
                EventCallback.Factory.Create(this, () => Save.Task));
            // An object parameter, which Blazor can't tell is unchanged, hands the popconfirm its parameters again.
            builder.AddComponentParameter(4, nameof(TbPopconfirm.Icon), Archive);
            builder.CloseComponent();
        }
    }
}
