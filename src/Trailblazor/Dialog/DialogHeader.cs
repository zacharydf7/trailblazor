using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

// A dialog's header, separate from the frame so the dialog can show or hide it while open. C#, so it can stay internal.
internal sealed class DialogHeader : ComponentBase, IDisposable
{
    [Parameter, EditorRequired]
    public DialogReference Dialog { get; set; } = default!;

    [Parameter, EditorRequired]
    public string TitleId { get; set; } = "";

    // The dialog element's id, for the close button's commandfor, so it closes the dialog before the page is interactive.
    [Parameter]
    public object? DialogId { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private Telemetry Telemetry { get; set; } = default!;

    protected override void OnInitialized()
    {
        Dialog.Changed += OnChanged;
        Dialog.HeaderContentChanged += OnChanged;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Dialog.Unnamed)
        {
            throw new InvalidOperationException(
                $"{Dialog.ContentType?.Name} has no title. Pass one, or have its DefaultTitle return one.");
        }

        // Without a header the title still names the dialog, as plain text so it adds no heading.
        if (!Dialog.ShowHeader)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "id", TitleId);
            builder.AddAttribute(2, "class", "tb-visually-hidden");
            builder.AddContent(3, Dialog.Title);
            builder.CloseElement();
            return;
        }

        var classes = Dialog.Options.Classes;
        builder.OpenElement(10, "header");
        builder.AddAttribute(11, "data-part", "header");
        builder.AddAttribute(12, "class", ClassNames.Join("tb-dialog__header", classes?.Header));
        builder.OpenElement(13, "h2");
        builder.AddAttribute(14, "id", TitleId);
        builder.AddAttribute(15, "data-part", "title");
        builder.AddAttribute(16, "class", ClassNames.Join("tb-dialog__title", classes?.Title));
        if (Dialog.TitleContent is { } title)
        {
            builder.AddContent(17, title);
        }
        else
        {
            builder.AddContent(18, Dialog.Title);
        }

        builder.CloseElement();
        if (Dialog.HeaderActions is { } actions)
        {
            builder.OpenElement(19, "div");
            builder.AddAttribute(20, "data-part", "header-actions");
            builder.AddAttribute(21, "class", "tb-dialog__header-actions");
            builder.AddContent(22, actions);
            builder.CloseElement();
        }

        var state = Dialog.WindowState;
        if (Dialog.Draggable && !Dialog.IsBlocking && state == DialogWindowState.Normal)
        {
            DialogMenus.AddMove(builder, 23, Dialog, Localizer);
        }

        if (Dialog.Minimizable)
        {
            var minimized = state == DialogWindowState.Minimized;
            AddWindowButton(builder, 31, "minimize", Dialog.Options.Attributes?.Minimize,
                Localizer[minimized ? TbStrings.DialogRestore : TbStrings.DialogMinimize],
                minimized ? BuiltInIcons.Restore : BuiltInIcons.Minus,
                minimized ? DialogWindowState.Normal : DialogWindowState.Minimized);
        }

        if (Dialog.Maximizable)
        {
            var maximized = state == DialogWindowState.Maximized;
            AddWindowButton(builder, 39, "maximize", Dialog.Options.Attributes?.Maximize,
                Localizer[maximized ? TbStrings.DialogRestore : TbStrings.DialogMaximize],
                maximized ? BuiltInIcons.Restore : BuiltInIcons.Maximize,
                maximized ? DialogWindowState.Normal : DialogWindowState.Maximized);
        }

        if (Dialog.CloseButton)
        {
            builder.OpenComponent<TbIconButton>(47);
            builder.AddMultipleAttributes(48, Dialog.Options.Attributes?.Close);
            builder.AddComponentParameter(49, nameof(TbIconButton.Icon), BuiltInIcons.Close);
            builder.AddComponentParameter(50, nameof(TbIconButton.AriaLabel), Localizer[TbStrings.DialogClose]);
            builder.AddComponentParameter(51, nameof(TbIconButton.Size), Size.Small);
            builder.AddComponentParameter(52, nameof(TbIconButton.OnClick),
                EventCallback.Factory.Create<MouseEventArgs>(this, CloseAsync));
            builder.AddComponentParameter(53, nameof(TbIconButton.Class), classes?.Close);
            builder.AddAttribute(54, "data-part", "close");
            if (!RendererInfo.IsInteractive)
            {
                builder.AddAttribute(55, "command", "close");
                builder.AddAttribute(56, "commandfor", DialogId);
            }

            builder.CloseComponent();
        }

        builder.CloseElement();
    }

    // Each button keeps its place as its label changes, so focus stays on it after a click.
    private void AddWindowButton(RenderTreeBuilder builder, int sequence, string part,
        IReadOnlyDictionary<string, object>? attributes, string label, Icon icon, DialogWindowState next)
    {
        builder.OpenComponent<TbIconButton>(sequence);
        builder.AddMultipleAttributes(sequence + 1, attributes);
        builder.AddComponentParameter(sequence + 2, nameof(TbIconButton.Icon), icon);
        builder.AddComponentParameter(sequence + 3, nameof(TbIconButton.AriaLabel), label);
        builder.AddComponentParameter(sequence + 4, nameof(TbIconButton.Size), Size.Small);
        builder.AddComponentParameter(sequence + 5, nameof(TbIconButton.OnClick),
            EventCallback.Factory.Create<MouseEventArgs>(this, () => SetWindowState(next)));
        builder.AddComponentParameter(sequence + 6, nameof(TbIconButton.Class), Dialog.Options.Classes?.WindowButton);
        builder.AddAttribute(sequence + 7, "data-part", part);
        builder.CloseComponent();
    }

    private void SetWindowState(DialogWindowState next)
    {
        if (Dialog.Options.TelemetryName is { } name)
        {
            var action = next switch
            {
                DialogWindowState.Minimized => "minimize",
                DialogWindowState.Maximized => "maximize",
                _ => "restore",
            };
            Telemetry.ControlUsed(name, DialogService.ComponentOf(Dialog), action);
        }

        Dialog.WindowState = next;
    }

    private Task CloseAsync() => Dialog.DismissAsync(DialogDismissReason.CloseButton);

    private void OnChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Dialog.Changed -= OnChanged;
        Dialog.HeaderContentChanged -= OnChanged;
    }
}
