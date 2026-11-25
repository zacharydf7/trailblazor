using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

// A submit button whose form attribute names a form follows that form, as a browser submits that form, wherever the
// button is.
public class TbFormOtherFormButtonTests : BunitContext
{
    public TbFormOtherFormButtonTests() => this.AddTrailblazor();

    [Fact]
    public async Task Button_ShouldNotShowBusy_WhenTheFormAroundItSubmitsButItsFormAttributeNamesAnother()
    {
        var cut = Render<TwoForms>();

        var submit = cut.Find("#a").SubmitAsync();

        Assert.DoesNotContain("Send B", Busy(cut));
        cut.Instance.Hold.SetResult();
        await submit;
    }

    [Fact]
    public async Task Button_ShouldShowBusy_WhenTheFormItsFormAttributeNamesSubmits()
    {
        var cut = Render<TwoForms>();

        var submit = cut.Find("#b").SubmitAsync();

        Assert.Equal(["Send B", "Outside B"], Busy(cut));
        cut.Instance.Hold.SetResult();
        await submit;
        Assert.Empty(Busy(cut));
    }

    [Fact]
    public void Button_ShouldBeDisabled_WhenTheFormItsFormAttributeNamesIsDisabled()
    {
        var cut = Render<TwoForms>(parameters => parameters.Add(forms => forms.DisableB, true));

        Assert.Equal(["Send B", "Outside B"],
            cut.FindAll("button[disabled]").Select(button => button.TextContent.Trim()));
    }

    [Fact]
    public async Task Button_ShouldFollowTheFormItNames_WhenTheFormComesAndGoesAfterIt()
    {
        var cut = Render<LateForm>();
        Assert.Null(cut.Find("button").GetAttribute("disabled"));

        // The form comes and goes in a render the button sits out, since none of its parameters change.
        await cut.InvokeAsync(() => cut.Instance.Show(true));
        Assert.NotNull(cut.Find("button").GetAttribute("disabled"));

        await cut.InvokeAsync(() => cut.Instance.Show(false));
        Assert.Null(cut.Find("button").GetAttribute("disabled"));
    }

    private static string[] Busy(IRenderedComponent<TwoForms> cut) =>
        [.. cut.FindAll("button[aria-busy=true]").Select(button => button.TextContent.Trim())];

    // Form a holds a submit button for form b; a submit button for form b also sits outside both forms.
    private sealed class TwoForms : ComponentBase
    {
        [Parameter]
        public bool DisableB { get; set; }

        public TaskCompletionSource Hold { get; } = new();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Form(builder, 0, "a", false, content =>
            {
                Button(content, 0, "Save A", null);
                Button(content, 10, "Send B", "b");
            });
            Form(builder, 10, "b", DisableB, content => content.AddContent(0, "Form b"));
            Button(builder, 20, "Outside B", "b");
        }

        private void Form(RenderTreeBuilder builder, int sequence, string id, bool disabled, RenderFragment content)
        {
            builder.OpenComponent<TbForm>(sequence);
            builder.AddComponentParameter(sequence + 1, "id", id);
            builder.AddComponentParameter(sequence + 2, nameof(TbForm.OnSubmit),
                EventCallback.Factory.Create<EditContext>(this, () => Hold.Task));
            builder.AddComponentParameter(sequence + 3, nameof(TbForm.Disabled), disabled);
            builder.AddComponentParameter(sequence + 4, nameof(TbForm.ChildContent), content);
            builder.CloseComponent();
        }

        private static void Button(RenderTreeBuilder builder, int sequence, string label, string? form)
        {
            builder.OpenComponent<TbButton>(sequence);
            builder.AddComponentParameter(sequence + 1, nameof(TbButton.Type), ButtonType.Submit);
            if (form is not null)
            {
                builder.AddComponentParameter(sequence + 2, "form", form);
            }

            builder.AddComponentParameter(sequence + 3, nameof(TbButton.ChildContent),
                (RenderFragment)(text => text.AddContent(0, label)));
            builder.CloseComponent();
        }
    }
    // A submit button for form b, and a disabled form b that comes and goes beside it.
    private sealed class LateForm : ComponentBase
    {
        private readonly RenderFragment _region;
        private bool _shown;

        public LateForm() => _region = builder =>
        {
            if (_shown)
            {
                builder.OpenComponent<TbForm>(0);
                builder.AddComponentParameter(1, "id", "b");
                builder.AddComponentParameter(2, nameof(TbForm.Disabled), true);
                builder.CloseComponent();
            }
        };

        public void Show(bool shown)
        {
            _shown = shown;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbButton>(0);
            builder.AddComponentParameter(1, nameof(TbButton.Type), ButtonType.Submit);
            builder.AddComponentParameter(2, "form", "b");
            builder.CloseComponent();
            builder.AddContent(3, _region);
        }
    }
}
