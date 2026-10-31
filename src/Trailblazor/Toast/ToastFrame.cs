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

    // A closed toast stays a moment to fade out, out of reach of the pointer and screen readers.
    [Parameter]
    public bool Leaving { get; set; }

    [Inject]
    private ToastService Toasts { get; set; } = default!;

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
        builder.AddAttribute(3, "data-leaving", Leaving ? "" : null);
        builder.AddAttribute(4, "inert", Leaving);
        builder.AddAttribute(5, "onmouseenter",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Hold(hovered: true)));
        builder.AddAttribute(6, "onmouseleave",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Hold(hovered: false)));
        builder.AddAttribute(7, "onfocusin",
            EventCallback.Factory.Create<FocusEventArgs>(this, () => Hold(focused: true)));
        builder.AddAttribute(8, "onfocusout",
            EventCallback.Factory.Create<FocusEventArgs>(this, () => Hold(focused: false)));
        if (Icon is { } icon)
        {
            builder.OpenComponent<TbIcon>(9);
            builder.AddComponentParameter(10, nameof(TbIcon.Icon), icon);
            builder.AddAttribute(11, "data-part", "icon");
            builder.AddAttribute(12, "class", "tb-toast__icon");
            builder.CloseComponent();
        }

        builder.OpenElement(13, "div");
        builder.AddAttribute(14, "class", "tb-toast__body");
        if (Toast.Options.Title is { } title && !string.IsNullOrWhiteSpace(title))
        {
            builder.OpenElement(15, "div");
            builder.AddAttribute(16, "data-part", "title");
            builder.AddAttribute(17, "class", "tb-toast__title");
            builder.AddContent(18, title);
            builder.CloseElement();
        }

        builder.OpenElement(19, "div");
        builder.AddAttribute(20, "data-part", "message");
        builder.AddAttribute(21, "class", "tb-toast__message");
        if (Toast.Options.Content is { } content)
        {
            builder.AddContent(22, content);
        }
        else
        {
            builder.AddContent(23, Toast.Message);
        }

        builder.CloseElement();
        builder.CloseElement();
        if (Toast.Options.Action is { } action)
        {
            builder.OpenComponent<TbButton>(24);
            builder.AddComponentParameter(25, nameof(TbButton.Variant), Variant.Text);
            builder.AddComponentParameter(26, nameof(TbButton.Size), Size.Small);
            builder.AddComponentParameter(27, nameof(TbButton.OnClick),
                EventCallback.Factory.Create<MouseEventArgs>(this, () => RunAsync(action)));
            builder.AddComponentParameter(28, nameof(TbButton.ChildContent),
                (RenderFragment)(text => text.AddContent(0, action.Text)));
            builder.AddAttribute(29, "data-part", "action");
            builder.CloseComponent();
        }

        builder.OpenComponent<TbIconButton>(30);
        builder.AddComponentParameter(31, nameof(TbIconButton.Icon), BuiltInIcons.Close);
        builder.AddComponentParameter(32, nameof(TbIconButton.AriaLabel), Localizer[TbStrings.ToastDismiss]);
        builder.AddComponentParameter(33, nameof(TbIconButton.Size), Size.Small);
        builder.AddComponentParameter(34, nameof(TbIconButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(
            this, () => Toasts.Close(Toast, ToastCloseReason.CloseButton)));
        builder.AddAttribute(35, "data-part", "close");
        builder.CloseComponent();
        builder.CloseElement();
    }

    // The tone's icon unless the toast names its own or none.
    private Icon? Icon => !Toast.Options.ShowIcon ? null : Toast.Options.Icon ?? BuiltInIcons.ForTone(Toast.Tone);

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
        _timer = Time.CreateTimer(_ => InvokeAsync(() => Toasts.Close(Toast, ToastCloseReason.TimedOut)), null,
            remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
    }

    private async Task RunAsync(ToastAction action)
    {
        Toasts.Close(Toast, ToastCloseReason.Action);
        await action.OnClick.InvokeAsync();
    }

    public void Dispose() => _timer?.Dispose();
}
