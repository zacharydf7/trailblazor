using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// The component a dialog opened from code shows. C#, so it can stay internal.
internal sealed class DialogContent : ComponentBase, IDisposable
{
    private bool _initialized;
    private IReadOnlyDictionary<string, object?> _parameters = new Dictionary<string, object?>();

    [Parameter, EditorRequired]
    public DialogReference Dialog { get; set; } = default!;

    // A re-render of the frame would push the dialog's parameters back in, undoing changes it made to them, so
    // only an update from the caller renders it again, with just the parameters the update names.
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (_initialized)
        {
            return Task.CompletedTask;
        }

        _initialized = true;
        return base.SetParametersAsync(parameters);
    }

    protected override void OnInitialized()
    {
        _parameters = Dialog.Parameters;
        Dialog.ParametersChanged += OnParametersChanged;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "DynamicComponent's Type is only given "
        + "ContentType, which keeps every member of the dialog's content.")]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DynamicComponent>(0);
        builder.AddComponentParameter(1, nameof(DynamicComponent.Type), Dialog.ContentType);
        builder.AddComponentParameter(2, nameof(DynamicComponent.Parameters),
            _parameters.ToDictionary(pair => pair.Key, pair => pair.Value!));
        builder.CloseComponent();
    }

    private void OnParametersChanged(IReadOnlyDictionary<string, object?> parameters) => InvokeAsync(() =>
    {
        _parameters = parameters;
        StateHasChanged();
    });

    public void Dispose() => Dialog.ParametersChanged -= OnParametersChanged;
}
