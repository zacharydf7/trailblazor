using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// The live regions the announcer writes to. TbRoot renders one so it exists before the first message, and each
// modal dialog has its own, because a modal hides the rest of the page from screen readers. Each host speaks the
// messages announced while its dialog was the topmost modal. C#, so it can stay internal.
internal sealed class AnnouncerHost : ComponentBase, IDisposable
{
    // A modal's live regions join the accessibility tree as it opens, and one that joins holding text isn't spoken.
    private bool _shown;

    // The modal dialog this host sits in, or null for the one in TbRoot.
    [Parameter]
    public DialogReference? Dialog { get; set; }

    [Inject]
    private Announcer Announcer { get; set; } = default!;

    protected override void OnInitialized()
    {
        Announcer.Hosts.Attach();
        Announcer.Changed += OnChanged;
    }

    internal void Show()
    {
        _shown = true;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "tb-visually-hidden");
        builder.AddAttribute(2, "data-part", "announcer");
        AddRegion(builder, 3, "status", "polite", Announcer.PoliteMessages);
        AddRegion(builder, 10, "alert", "assertive", Announcer.AssertiveMessages);
        builder.CloseElement();
    }

    private void AddRegion(RenderTreeBuilder builder, int sequence, string role, string live,
        IReadOnlyList<Announcement> messages)
    {
        builder.OpenElement(sequence, "div");
        builder.AddAttribute(sequence + 1, "role", role);
        builder.AddAttribute(sequence + 2, "aria-live", live);
        builder.AddAttribute(sequence + 3, "data-part", live);
        foreach (var announcement in messages.Where(announcement => (Dialog is null || _shown)
            && announcement.Dialog == Dialog))
        {
            builder.OpenElement(sequence + 4, "p");
            builder.SetKey(announcement.Handle);
            builder.AddContent(sequence + 5, announcement.Message);
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private void OnChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Announcer.Hosts.Detach();
        Announcer.Changed -= OnChanged;
    }
}
