using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// The dialog behind IDialogService.PromptAsync: one labeled line of text, checked before it closes. C#, so it can stay
// internal.
internal sealed class PromptDialog : TbDialogContent<string>
{
    private string _text = "";
    private string? _error;

    [Parameter]
    public string Label { get; set; } = "";

    [Parameter]
    public string? Initial { get; set; }

    [Parameter]
    public Func<string, string?>? Validate { get; set; }

    [Parameter]
    public string ConfirmText { get; set; } = "";

    [Parameter]
    public string CancelText { get; set; } = "";

    protected override void OnInitialized() => _text = Initial ?? "";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbTextField<string>>(0);
        builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), Label);
        builder.AddComponentParameter(2, nameof(TbTextField<string>.Value), _text);
        builder.AddComponentParameter(3, nameof(TbTextField<string>.ValueChanged),
            EventCallback.Factory.Create<string>(this, Changed));
        builder.AddComponentParameter(4, nameof(TbTextField<string>.UpdateOnInput), true);
        builder.AddComponentParameter(5, nameof(TbTextField<string>.Error), _error);
        builder.AddComponentParameter(6, nameof(TbTextField<string>.AutoFocus), true);
        builder.AddComponentParameter(7, nameof(TbTextField<string>.OnEnter),
            EventCallback.Factory.Create(this, SubmitAsync));
        builder.CloseComponent();
        builder.OpenComponent<TbDialogActions>(8);
        builder.AddComponentParameter(9, nameof(TbDialogActions.ChildContent), (RenderFragment)(actions =>
        {
            DialogButtons.Add(actions, 0, this, CancelText, Variant.Outlined, Tone.Neutral, Dialog.CancelAsync);
            DialogButtons.Add(actions, 10, this, ConfirmText, Variant.Filled, Tone.Primary, SubmitAsync);
        }));
        builder.CloseComponent();
    }

    // Editing the text clears the last check's message until it's submitted again.
    private void Changed(string? text) => (_text, _error) = (text ?? "", null);

    private async Task SubmitAsync()
    {
        // A blank message accepts the text, as no message does.
        _error = Validate?.Invoke(_text) is { } message && !string.IsNullOrWhiteSpace(message) ? message : null;
        if (_error is null)
        {
            await Dialog.CloseAsync(_text);
        }
    }
}
