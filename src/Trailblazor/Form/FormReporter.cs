using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// Renders nothing. After an edit the form re-renders it, so the report waits for the edit's render batch; if that batch
// renders the form too, only one of them reports.
internal sealed class FormReporter : ComponentBase
{
    private bool _queued;

    [Parameter, EditorRequired]
    public TbForm Form { get; set; } = default!;

    internal void Queue()
    {
        _queued = true;
        StateHasChanged();
    }

    // It renders with the form too, but reports only when an edit queued it.
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_queued)
        {
            return Task.CompletedTask;
        }

        _queued = false;
        return Form.ReportIfStaleAsync();
    }
}
