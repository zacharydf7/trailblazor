using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

// One visible toast and its timer, which pauses while the pointer or focus is on it. C#, so it can stay internal.
internal sealed class ToastFrame : ComponentBase, IDisposable
{
    private ITimer? _timer;
    private long _startedAt;
    private TimeSpan? _remaining;
    private int _version = -1;
    private bool _hovered;
    private bool _focused;

    [Parameter, EditorRequired]
    public ToastReference Toast { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (Toast.Version == _version)
        {
            return;
        }

        // New content gets the full time again.
        _version = Toast.Version;
        _timer?.Dispose();
        _timer = null;
        _remaining = Toast.Lifetime;
        Resume();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "li");
        builder.AddAttribute(1, "data-part", "toast");
        builder.AddAttribute(2, "class", ClassNames.Join("tb-toast", ClassNames.Modifier("tb-toast", Toast.Tone)));
        builder.AddAttribute(3, "onmouseenter",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Hold(hovered: true)));
        builder.AddAttribute(4, "onmouseleave",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Hold(hovered: false)));
        builder.AddAttribute(5, "onfocusin", EventCallback.Factory.Create<FocusEventArgs>(this, () => Hold(focused: true)));
        builder.AddAttribute(6, "onfocusout",
            EventCallback.Factory.Create<FocusEventArgs>(this, () => Hold(focused: false)));
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "data-part", "message");
        builder.AddAttribute(9, "class", "tb-toast__message");
        builder.AddContent(10, Toast.Message);
        builder.CloseElement();
        if (Toast.Options.Action is { } action)
        {
            builder.OpenComponent<TbButton>(11);
            builder.AddComponentParameter(12, nameof(TbButton.Variant), Variant.Text);
            builder.AddComponentParameter(13, nameof(TbButton.Size), Size.Small);
            builder.AddComponentParameter(14, nameof(TbButton.OnClick),
                EventCallback.Factory.Create<MouseEventArgs>(this, () => RunAsync(action)));
            builder.AddComponentParameter(15, nameof(TbButton.ChildContent),
                (RenderFragment)(content => content.AddContent(0, action.Text)));
            builder.AddAttribute(16, "data-part", "action");
            builder.CloseComponent();
        }

        builder.OpenComponent<TbIconButton>(17);
        builder.AddComponentParameter(18, nameof(TbIconButton.Icon), BuiltInIcons.Close);
        builder.AddComponentParameter(19, nameof(TbIconButton.AriaLabel), Localizer[TbStrings.ToastDismiss]);
        builder.AddComponentParameter(20, nameof(TbIconButton.Size), Size.Small);
        builder.AddComponentParameter(21, nameof(TbIconButton.OnClick),
            EventCallback.Factory.Create<MouseEventArgs>(this, Toast.Dismiss));
        builder.AddAttribute(22, "data-part", "close");
        builder.CloseComponent();
        builder.CloseElement();
    }

    private void Hold(bool? hovered = null, bool? focused = null)
    {
        _hovered = hovered ?? _hovered;
        _focused = focused ?? _focused;
        if (_hovered || _focused)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }

    private void Pause()
    {
        if (_timer is null)
        {
            return;
        }

        _timer.Dispose();
        _timer = null;
        _remaining -= Time.GetElapsedTime(_startedAt);
    }

    private void Resume()
    {
        // Static rendering has no circuit to close the toast on later.
        if (_timer is not null || _remaining is not { } remaining || _hovered || _focused
            || !RendererInfo.IsInteractive)
        {
            return;
        }

        _startedAt = Time.GetTimestamp();
        _timer = Time.CreateTimer(_ => InvokeAsync(Toast.Dismiss), null,
            remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
    }

    private async Task RunAsync(ToastAction action)
    {
        Toast.Dismiss();
        await action.OnClick.InvokeAsync();
    }

    public void Dispose() => _timer?.Dispose();
}
