using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbFormSubmittingTests : BunitContext
{
    public TbFormSubmittingTests() => this.AddTrailblazor();

    [Theory]
    [InlineData(nameof(TbForm.OnSubmit))]
    [InlineData(nameof(TbForm.OnValidSubmit))]
    [InlineData(nameof(TbForm.OnInvalidSubmit))]
    public async Task Submit_ShouldReportSubmittingUntilTheHandlerFinishes_WhenGivenEachHandler(string handler)
    {
        var cut = Render<HeldForm>(parameters => parameters.Add(form => form.Handler, handler));
        var host = cut.Instance;

        var submit = cut.Find("form").SubmitAsync();

        Assert.Equal([true], host.Reports);
        Assert.True(cut.FindComponent<TbForm>().Instance.IsSubmitting);
        host.Hold.SetResult();
        await submit;
        Assert.Equal([true, false], host.Reports);
        Assert.False(cut.FindComponent<TbForm>().Instance.IsSubmitting);
    }

    [Fact]
    public async Task Submit_ShouldShowTheSubmitButtonsBusy_WhenTheHandlerIsRunning()
    {
        var cut = Render<HeldForm>();

        var submit = cut.Find("form").SubmitAsync();

        Assert.Equal(["Save", "Send"], Busy(cut));
        Assert.All(cut.FindAll("button[aria-busy=true]"), button =>
        {
            Assert.Equal("true", button.GetAttribute("aria-disabled"));
            Assert.False(button.HasAttribute("disabled"));
            Assert.NotNull(button.QuerySelector("[data-part=spinner]"));
        });
        cut.Instance.Hold.SetResult();
        await submit;
        Assert.Empty(Busy(cut));
        Assert.Empty(cut.FindAll("[data-part=spinner]"));
    }

    [Fact]
    public async Task Submit_ShouldLeaveTheSubmitButtonsAsTheyAre_WhenShowSubmittingIsOff()
    {
        var cut = Render<HeldForm>(parameters => parameters.Add(form => form.ShowSubmitting, false));

        var submit = cut.Find("form").SubmitAsync();

        Assert.Empty(Busy(cut));
        Assert.Equal([true], cut.Instance.Reports);
        cut.Instance.Hold.SetResult();
        await submit;
    }

    [Fact]
    public async Task Submit_ShouldBeDropped_WhenTheFormIsStillHandlingOne()
    {
        var cut = Render<HeldForm>(parameters => parameters.Add(form => form.ShowSubmitting, false));
        var host = cut.Instance;
        var first = cut.Find("form").SubmitAsync();

        var second = cut.Find("form").SubmitAsync();

        Assert.Equal(1, host.Handled);
        host.Hold.SetResult();
        await Task.WhenAll(first, second);
        await cut.Find("form").SubmitAsync();
        Assert.Equal(2, host.Handled);
    }

    [Fact]
    public async Task Click_ShouldBeIgnored_WhenTheFormIsHandlingASubmit()
    {
        var cut = Render<HeldForm>();
        var submit = cut.Find("form").SubmitAsync();

        cut.Find("button[type=submit]").Click();

        Assert.Equal(0, cut.Instance.Clicks);
        cut.Instance.Hold.SetResult();
        await submit;
        cut.Find("button[type=submit]").Click();
        Assert.Equal(1, cut.Instance.Clicks);
    }

    [Fact]
    public async Task Submit_ShouldStopSubmitting_WhenTheHandlerThrows()
    {
        var cut = Render<HeldForm>();
        var host = cut.Instance;
        var submit = cut.Find("form").SubmitAsync();

        host.Hold.SetException(new InvalidOperationException("The save failed."));

        await submit;
        Assert.IsType<InvalidOperationException>(await Renderer.UnhandledException);
        Assert.Equal([true, false], host.Reports);
        // bUnit throws the failure it kept at the next render, and then forgets it.
        Assert.Throws<InvalidOperationException>(() => cut.Render());
        Assert.Empty(Busy(cut));
    }

    [Fact]
    public void Submit_ShouldNotReportSubmitting_WhenTheFormIsDisabled()
    {
        var cut = Render<HeldForm>(parameters => parameters.Add(form => form.Disabled, true));

        cut.Find("form").Submit();

        Assert.Empty(cut.Instance.Reports);
        Assert.Equal(0, cut.Instance.Handled);
    }

    private static string[] Busy(IRenderedComponent<HeldForm> cut) =>
        [.. cut.FindAll("button[aria-busy=true]").Select(button =>
            button.GetAttribute("aria-label") ?? button.TextContent.Trim())];

    // A form whose handler waits on Hold the first time it runs, as a save would wait on a server.
    private sealed class HeldForm : ComponentBase
    {
        [Parameter]
        public string Handler { get; set; } = nameof(TbForm.OnValidSubmit);

        [Parameter]
        public bool ShowSubmitting { get; set; } = true;

        [Parameter]
        public bool Disabled { get; set; }

        public TaskCompletionSource Hold { get; } = new();

        public List<bool> Reports { get; } = [];

        public int Handled { get; private set; }

        public int Clicks { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbForm>(0);
            builder.AddComponentParameter(1, Handler, EventCallback.Factory.Create<EditContext>(this, HandleAsync));
            builder.AddComponentParameter(2, nameof(TbForm.ShowSubmitting), ShowSubmitting);
            builder.AddComponentParameter(3, nameof(TbForm.Disabled), Disabled);
            builder.AddComponentParameter(4, nameof(TbForm.IsSubmittingChanged),
                EventCallback.Factory.Create<bool>(this, Reports.Add));
            builder.AddComponentParameter(5, nameof(TbForm.ChildContent), (RenderFragment)(content =>
            {
                // Left empty, so the form is invalid when its handler is OnInvalidSubmit.
                content.OpenComponent<TbTextField<string>>(0);
                content.AddComponentParameter(1, nameof(TbTextField<string>.Label), "Name");
                content.AddComponentParameter(2, nameof(TbTextField<string>.Required),
                    Handler == nameof(TbForm.OnInvalidSubmit));
                content.CloseComponent();
                Button(content, 3, "Save", ButtonType.Submit);
                Button(content, 4, "Reset", ButtonType.Reset);
                Button(content, 5, "Preview", ButtonType.Button);
                content.OpenComponent<TbIconButton>(6);
                content.AddComponentParameter(7, nameof(TbIconButton.Icon),
                    new Icon("<path d=\"M4 4h16v16H4z\" />"));
                content.AddComponentParameter(8, nameof(TbIconButton.AriaLabel), "Send");
                content.AddComponentParameter(9, nameof(TbIconButton.Type), ButtonType.Submit);
                content.CloseComponent();
            }));
            builder.CloseComponent();
        }

        private void Button(RenderTreeBuilder builder, int sequence, string label, ButtonType type)
        {
            builder.OpenComponent<TbButton>(sequence);
            builder.AddComponentParameter(0, nameof(TbButton.Type), type);
            builder.AddComponentParameter(1, nameof(TbButton.OnClick),
                EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => Clicks++));
            builder.AddComponentParameter(2, nameof(TbButton.ChildContent),
                (RenderFragment)(text => text.AddContent(0, label)));
            builder.CloseComponent();
        }

        private async Task HandleAsync()
        {
            Handled++;
            await Hold.Task;
        }
    }
}
