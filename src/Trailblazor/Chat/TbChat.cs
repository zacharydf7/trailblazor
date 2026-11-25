using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A conversation: messages from several people, who is typing, files sent with messages, replies that stream
/// in, and a box to write and send a message. Screen readers and keyboards get the WAI-ARIA feed pattern: Page Down and
/// Page Up move between messages, and new messages from others are read out.</summary>
public partial class TbChat
{
    private readonly string _id = $"tb-chat-{Guid.NewGuid():N}";
    private readonly List<IBrowserFile> _files = [];
    // Streamed replies still arriving, read out once they end.
    private readonly HashSet<string> _streaming = [];
    // The newest message the chat has seen; only messages after it are news, not history loaded before it.
    private string? _newest;
    private bool _listened;
    private bool _sending;
    private ElementReference _root;
    // Set once the script has attached; the feed's keys, Enter to send and keeping the newest message in view work
    // from then on, and data-ready says so.
    private IJSObjectReference? _handle;
    private string _draft = "";
    private int _commits;
    private bool _disposed;

    /// <summary>Names the conversation for screen readers, such as "Trip planning". Required.</summary>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = "";

    /// <summary>The messages, oldest first. A message with an <see cref="ChatMessage.Id"/> not seen before is read out
    /// when it's from someone else, and a streaming one when it stops streaming.</summary>
    [Parameter]
    public IReadOnlyList<ChatMessage> Messages { get; set; } = [];

    /// <summary>The user's own <see cref="ChatAuthor.Id"/>. Their messages sit at the end of the line, and aren't read
    /// out.</summary>
    [Parameter]
    public string? CurrentAuthorId { get; set; }

    /// <summary>The people typing now, named under the messages. Leave out the user.</summary>
    [Parameter]
    public IReadOnlyList<ChatAuthor> TypingAuthors { get; set; } = [];

    /// <summary>Draws a message's body in place of its <see cref="ChatMessage.Text"/>, such as for formatted text.
    /// </summary>
    [Parameter]
    public RenderFragment<ChatMessage>? MessageTemplate { get; set; }

    /// <summary>Raised, and awaited, when the user sends what they wrote, by pressing Enter or the send button. The
    /// box and the chosen files clear as soon as it's raised, and the user can't send or attach again until it has run.
    /// Shift+Enter starts a new line instead.</summary>
    [Parameter]
    public EventCallback<ChatSend> OnSend { get; set; }

    /// <summary>Shows the box to write a message, with its send button. On by default; turn it off for a chat people
    /// only read.</summary>
    [Parameter]
    public bool ShowComposer { get; set; } = true;

    /// <summary>Shows a button that chooses files to send with a message. Each choice replaces the files chosen before
    /// it. Off by default.</summary>
    [Parameter]
    public bool AllowAttachments { get; set; }

    /// <summary>Text shown in the empty box, such as "Write to the group".</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Turns the box and its buttons off, such as while the app is offline.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The time zone each message's time shows in. Left null, it uses a <see cref="TimeZoneInfo"/> cascaded
    /// around the chat, then <see cref="TimeSettings.TimeZone"/>, then the <see cref="TimeProvider"/>'s local zone,
    /// which on Blazor Server is the server's.</summary>
    [Parameter]
    public TimeZoneInfo? TimeZone { get; set; }

    [CascadingParameter]
    private TimeZoneInfo? CascadedTimeZone { get; set; }

    /// <summary>Replaces the words the chat shows and says to screen readers.</summary>
    [Parameter]
    public ChatLabels Labels { get; set; } = new();

    /// <summary>A label this chat's use is counted under, such as <c>"support"</c>, so the app can see how people use
    /// it through the <c>Trailblazor</c> meter: counted in <c>trailblazor.control.shown</c> when it first renders, and
    /// in <c>trailblazor.control.used</c> each time a message is sent. Nothing the user writes is recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    private string Text(string? given, string key) => given ?? Localizer[key];

    private string Time(DateTimeOffset sent) => TimeZoneInfo.ConvertTime(sent,
        ShownTime.Zone(TimeZone, CascadedTimeZone, Services)).ToString("t", CultureInfo.CurrentCulture);

    private string? TypingText => TypingAuthors.Count switch
    {
        0 => null,
        1 => Format(Text(Labels.Typing, TbStrings.ChatTyping), TypingAuthors[0].Name),
        _ => Format(Text(Labels.TypingMany, TbStrings.ChatTypingMany),
            string.Join(", ", TypingAuthors.SkipLast(1).Select(author => author.Name)), TypingAuthors[^1].Name),
    };

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new InvalidOperationException($"{nameof(TbChat)} needs an {nameof(AriaLabel)}.");
        }

        // What's there when the chat first draws, and older messages loaded before the newest, are history. When the
        // chat had none, only the newest of what arrives can be news, so a history loaded late isn't read out whole.
        var newest = _newest is null ? -1 : Messages.Select(message => message.Id).ToList().IndexOf(_newest);
        var start = !_listened || (_newest is not null && newest < 0) ? Messages.Count
            : _newest is null ? Messages.Count - 1 : newest + 1;
        for (var at = 0; at < Messages.Count; at++)
        {
            var message = Messages[at];
            var news = at >= start;
            if (message.IsStreaming)
            {
                if (news)
                {
                    _streaming.Add(message.Id);
                }
            }
            else if ((news || _streaming.Remove(message.Id)) && message.Author.Id != CurrentAuthorId)
            {
                Announcer.Polite($"{message.Author.Name}: {message.Text}");
            }
        }

        _newest = Messages.Count > 0 ? Messages[^1].Id : _newest;
        _listened = true;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Telemetry.Rendered(TelemetryName);
        }

        if (!firstRender || !RendererInfo.IsInteractive)
        {
            return;
        }

        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/chat.js");
            if (_disposed)
            {
                return;
            }

            var handle = await module.InvokeAsync<IJSObjectReference?>("createChat", _root);
            if (_disposed && handle is not null)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
            StateHasChanged();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the chat went with it.
        }
    }

    // Built in C# for SetUpdatesAttributeName("value"), so a render for an earlier keystroke can't drop fast-typed
    // characters, and so the handler stays the same across renders.
    private RenderFragment Box => builder =>
    {
        builder.OpenElement(0, "textarea");
        builder.AddAttribute(1, "data-part", "input");
        builder.AddAttribute(2, "aria-label", Text(Labels.Message, TbStrings.ChatMessage));
        builder.AddAttribute(3, "rows", 1);
        builder.AddAttribute(4, "placeholder", Placeholder);
        builder.AddAttribute(5, "disabled", Disabled);
        builder.AddAttribute(6, "class", "tb-chat__input");
        builder.AddAttribute(7, "value", _draft);
        builder.AddAttribute(8, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, Type));
        builder.SetUpdatesAttributeName("value");
        builder.CloseElement();
    };

    // The library's own file input, which the testing helpers know to look for; C#, since it's internal.
    private RenderFragment Files => builder =>
    {
        builder.OpenComponent<ListeningInputFile>(0);
        builder.AddComponentParameter(1, nameof(InputFile.OnChange),
            EventCallback.Factory.Create<InputFileChangeEventArgs>(this, Choose));
        builder.AddAttribute(2, "id", $"{_id}-files");
        builder.AddAttribute(3, "multiple", true);
        // A new choice makes the browser forget the files of a send still being handled.
        builder.AddAttribute(4, "disabled", Disabled || _sending);
        builder.AddAttribute(5, "class", "tb-visually-hidden");
        builder.CloseComponent();
    };

    private void Type(ChangeEventArgs args) => _draft = args.Value as string ?? "";

    // A new choice replaces the last, since the browser forgets a choice's files once another is made.
    private void Choose(InputFileChangeEventArgs args)
    {
        _files.Clear();
        _files.AddRange(args.GetMultipleFiles(int.MaxValue));
    }

    // Counts every send, refused or not, once it's done, so a test can wait for the app's answer. The box clears at
    // once, so the user can write the next message, but a second send before the first has been handled is refused.
    private async Task SendAsync()
    {
        var text = _draft.Trim();
        try
        {
            if (_sending || Disabled || (text.Length == 0 && _files.Count == 0))
            {
                return;
            }

            _sending = true;
            ChatSend send = new(text, [.. _files]);
            _draft = "";
            _files.Clear();
            Telemetry.Used(TelemetryName, "send");
            try
            {
                await OnSend.InvokeAsync(send);
            }
            finally
            {
                _sending = false;
            }
        }
        finally
        {
            _commits++;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }
    }
}
