using System.Globalization;
using System.Runtime.ExceptionServices;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Trailblazor.Internal.Events;

namespace Trailblazor.Testing;

/// <summary>bUnit helpers that find Trailblazor components by their labels and names, as a user would, mostly with the
/// same names as the Playwright helpers in Trailblazor.Testing.Playwright.</summary>
public static class TrailblazorMarkupExtensions
{
    /// <summary>The Trailblazor components in this rendered markup.</summary>
    public static TrailblazorMarkup Tb<TComponent>(this IRenderedComponent<TComponent> rendered)
        where TComponent : IComponent =>
        new(selector => rendered.FindAll(selector), new ScriptCalls(rendered.Services, rendered.InvokeAsync,
            done => rendered.WaitForStateAsync(done), () => ((IRenderedComponent<IComponent>)rendered)
                .FindComponents<ListeningInputFile>().Select(input => (IRenderedComponent<InputFile>)input),
            () => rendered.Render()));
}

// Calls the .NET object a component handed its script, as the script would, for what only a script sees, such as
// where a drag drops.
internal sealed class ScriptCalls(IServiceProvider services, Func<Func<Task>, Task> invoke,
    Func<Func<bool>, Task> waitFor, Func<IEnumerable<IRenderedComponent<InputFile>>> inputFiles, Action render)
{
    // The drag events of the sortable list or grid with this accessible name, among those still in use.
    public SortableEvents Sortable(string? name)
    {
        var events = Js.Invocations["attachSortable"]
            .Select(call => call.Arguments.ElementAtOrDefault(1) as DotNetObjectReference<SortableEvents>)
            .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value)
            .Where(handed => handed.Name() == name).Distinct().ToList();
        return events.Count == 1 ? events[0] : throw new InvalidOperationException(events.Count == 0
            ? "The component's script hasn't attached, as it does once the component renders interactively."
            : $"{events.Count} sortable lists or grids are named \"{name}\"; give each its own name to drag in it.");
    }

    // The open dialog element's frame, as handed to the dialog script, which calls it back when Escape is pressed.
    public DialogFrame Dialog(IElement dialog) => Js.Invocations["openDialog"]
        .Select(call => call.Arguments.ElementAtOrDefault(1) as DotNetObjectReference<DialogFrame>)
        .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value)
        .FirstOrDefault(frame => frame.TitleId == dialog.GetAttribute("aria-labelledby"))
            ?? throw new InvalidOperationException("The dialog's script hasn't attached, as it does once the dialog "
                + "renders interactively.");

    // The events the menu script calls back for the menu that opens the list with this id.
    public MenuEvents Menu(string? listId) => Js.Invocations["createMenu"]
        .Select(call => call.Arguments.ElementAtOrDefault(3) as DotNetObjectReference<MenuEvents>)
        .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value)
        .LastOrDefault(events => events.ListId == listId)
            ?? throw new InvalidOperationException("The menu's script hasn't attached, as it does once the menu "
                + "renders interactively.");

    // The reports the splitter script sends for the splitter that holds the pane with this id, as its panes resize.
    public SplitterReport Splitter(string pane) => Js.Invocations["attachSplitter"]
        .Select(call => call.Arguments.ElementAtOrDefault(1) as DotNetObjectReference<SplitterReport>)
        .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value)
        .LastOrDefault(report => report.Holds(pane))
            ?? throw new InvalidOperationException("The splitter's script hasn't attached, as it does once the "
                + "splitter renders interactively.");

    // The reports the dock script sends for the dock that holds the tab with this element id.
    public DockReport Dock(string tab) => Js.Invocations["attachDock"]
        .Select(call => call.Arguments.ElementAtOrDefault(1) as DotNetObjectReference<DockReport>)
        .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value)
        .LastOrDefault(report => report.Holds(tab))
            ?? throw new InvalidOperationException("The dock's script hasn't attached, as it does once the dock "
                + "renders interactively.");

    // The events the popover script calls back for the panel with this id, which a popover has only when a bound Open
    // or its telemetry has to hear it open and close.
    public PopoverEvents? Popover(string panelId) => Js.Invocations["createPopover"]
        .Select(call => call.Arguments.ElementAtOrDefault(4) as DotNetObjectReference<PopoverEvents>)
        .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value)
        .LastOrDefault(events => events.PanelId == panelId);

    // The toggle the select script calls back for the select whose combobox has this id, as its list opens and closes.
    public SelectToggle Select(string? comboboxId) => Js.Invocations["createSelect"]
        .Select(call => call.Arguments.ElementAtOrDefault(2) as DotNetObjectReference<SelectToggle>)
        .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value)
        .LastOrDefault(toggle => toggle.ComboboxId == comboboxId)
            ?? throw new InvalidOperationException("The select's script hasn't attached, as it does once the select "
                + "renders interactively.");

    // The dismissable layer made for this element, which the layer script calls back when Escape is pressed, or null
    // when the component made none.
    public Trailblazor.Primitives.DismissableLayer? Layer(IElement element) => Js.Invocations["createDismissableLayer"]
        .Where(call => call.Arguments.ElementAtOrDefault(0) is ElementReference reference
            && reference.Id == element.GetAttribute("blazor:elementReference"))
        .Select(call =>
            call.Arguments.ElementAtOrDefault(2) as DotNetObjectReference<Trailblazor.Primitives.DismissableLayer>)
        .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value).LastOrDefault();

    // What the text script reports for this text, whose box it measures. bUnit's markup loses the element's reference
    // once the text re-renders with other attributes, so then it falls back to the only text being measured.
    public TextCut Text(IElement text)
    {
        var id = text.GetAttribute("blazor:elementReference");
        var live = Js.Invocations["watchTextCut"]
            .Where(call => string.IsNullOrEmpty(id)
                || (call.Arguments.ElementAtOrDefault(0) is ElementReference reference && reference.Id == id))
            .Select(call => call.Arguments.ElementAtOrDefault(1) as DotNetObjectReference<TextCut>)
            .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value).Distinct().ToList();
        return live.Count == 1 ? live[0] : throw new InvalidOperationException(live.Count == 0
            ? "The text's script measures its box only with MaxLines and ShowFullText, once the text renders "
                + "interactively."
            : "Several texts are measured and this one has re-rendered, so its script can't be found. Call "
                + "SetCutAsync on this text before it re-renders.");
    }
    // The events the mention script calls back for this field's input, as a trigger is typed and a suggestion chosen.
    public MentionInputEvents MentionInput(IElement input) => Js.Invocations["createMentionInput"]
        .Select(call => call.Arguments.ElementAtOrDefault(2) as DotNetObjectReference<MentionInputEvents>)
        .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value)
        .LastOrDefault(events => events.InputId == input.Id)
            ?? throw new InvalidOperationException("The mention input's script hasn't attached, as it does once the "
                + "field renders interactively.");

    // The events the rich text editor's script calls back as a trigger is typed in this editor and a suggestion chosen.
    public MentionInputEvents RichTextMentions(IElement editor) => Js.Invocations["setMentions"]
        .Select(call => call.Arguments.ElementAtOrDefault(1) as DotNetObjectReference<MentionInputEvents>)
        .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value)
        .LastOrDefault(events => events.InputId == editor.Id)
            ?? throw new InvalidOperationException("The editor suggests no mentions: it needs MentionTriggers and "
                + "a MentionSearch or MentionItems, and its script attaches once it renders interactively.");

    // The HTML a rich text editor handed its script to put in while this call ran, or null when it handed none.
    public async Task<string?> RichTextInsertAsync(Func<Task> call)
    {
        var before = Js.Invocations["insertCommand"].Count;
        await InvokeAsync(call);
        return Js.Invocations["insertCommand"].Skip(before).Select(put => put.Arguments[0] as string).LastOrDefault();
    }

    // The events the speech recognition script calls back for this dictation button, matched by its id.
    public SpeechToTextEvents SpeechToText(IElement button) => Js.Invocations["attachSpeechToText"]
        .Select(call => call.Arguments.ElementAtOrDefault(1) as DotNetObjectReference<SpeechToTextEvents>)
        .Where(handed => handed is not null && IsLive(handed)).Select(handed => handed!.Value)
        .LastOrDefault(events => events.ButtonId == button.Id)
            ?? throw new InvalidOperationException("The dictation button's script hasn't attached, as it does once "
                + "the button renders interactively.");

    // A link goes to its address, as in a browser, unless it opens in a new tab and leaves this page where it is.
    public void Follow(IElement action)
    {
        if (action.LocalName == "a" && action.GetAttribute("href") is { } href
            && action.GetAttribute("target") != "_blank")
        {
            services.GetRequiredService<NavigationManager>().NavigateTo(href);
        }
    }

    // The file input with this id, which the browser hands the files a user picks or drops.
    public IRenderedComponent<InputFile> InputFile(string id) => inputFiles()
        .SingleOrDefault(input => input.Instance.AdditionalAttributes?.TryGetValue("id", out var given) == true
            && Equals(given, id))
        ?? throw new InvalidOperationException("The file input isn't in the rendered component.");

    // A component hands what a script's call throws to the renderer, unless an error boundary took it. bUnit keeps
    // such a failure and throws it as the component next renders, so that render is made here: the test gets the
    // failure where it made the call, once. A failure after an earlier one arrives in a new task.
    public async Task InvokeAsync(Func<Task> work)
    {
        var renderer = services.GetRequiredService<BunitContext>().Renderer;
        var before = renderer.UnhandledException;
        var failedBefore = before.IsCompleted;
        await invoke(work);
        var after = renderer.UnhandledException;
        if (after.IsCompleted && (!failedBefore || !ReferenceEquals(after, before)))
        {
            render();
            ExceptionDispatchInfo.Capture(await after).Throw();
        }
    }

    public Task WaitForAsync(Func<bool> done) => waitFor(done);

    // The closed dialogs the app has answered, as TbRoot's dialog host counts them for the Playwright helpers.
    public int AnsweredDialogs => Dialogs.Answered;

    // A TbDialog's answer is counted after its OpenChanged, which may render nothing, so this listens to the service
    // rather than waiting for a render.
    public async Task WaitForAnswerAsync(int answered)
    {
        var counted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnChanged()
        {
            if (Dialogs.Answered > answered)
            {
                counted.TrySetResult();
            }
        }

        Dialogs.Changed += OnChanged;
        try
        {
            OnChanged();
            await counted.Task.WaitAsync(BunitContext.DefaultWaitTimeout);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException("The app didn't finish answering the dialog before the wait timed out.");
        }
        finally
        {
            Dialogs.Changed -= OnChanged;
        }
    }

    private DialogService Dialogs => services.GetRequiredService<DialogService>();

    private BunitJSInterop Js => services.GetService<BunitJSInterop>() ?? throw new InvalidOperationException(
        "Call AddTrailblazor on the test's BunitContext before rendering.");

    // A component disposes its reference when it goes, after which reading it throws.
    private static bool IsLive<T>(DotNetObjectReference<T> handed)
        where T : class
    {
        try
        {
            _ = handed.Value;
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }
}

/// <summary>What every field's driver reads: the messages the field shows and whether it's marked invalid.</summary>
public abstract class TbFieldDriver
{
    private readonly Func<IElement> _control;

    private protected TbFieldDriver(Func<IElement> control) => _control = control;

    /// <summary>The field's first error message, such as "Name is required.", or null when it shows none.</summary>
    public string? Error => Errors.FirstOrDefault();

    /// <summary>The error messages the field shows, in order.</summary>
    public IReadOnlyList<string> Errors
    {
        get
        {
            // The control is described by the field's error, which holds each message in a part of its own.
            var control = Described(_control());
            var top = control;
            while (top.ParentElement is { } parent)
            {
                top = parent;
            }

            return (control.GetAttribute("aria-describedby") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => top.QuerySelector($"[id='{id}'][data-part=error]")).OfType<IElement>()
                .SelectMany(error => error.QuerySelectorAll("[data-part=message]"))
                .Select(message => message.TextContent.Trim()).ToList();
        }
    }

    /// <summary>Whether the field's control, or a part of it such as a date range's end, is marked invalid through
    /// <c>aria-invalid</c>, which screen readers announce.</summary>
    public bool Invalid => _control() is var control
        && (control.GetAttribute("aria-invalid") == "true" || control.QuerySelector("[aria-invalid=true]") is not null);

    /// <summary>Moves focus out of the field, as tabbing away does, which raises its <c>OnBlur</c>. Does nothing when
    /// the field has no <c>OnBlur</c>, since that's all leaving it raises.</summary>
    public async Task LeaveAsync()
    {
        // Only the field's own root raises OnBlur, and only when the app set one; the nearest root may instead be a
        // grid's editor or row. Focus going nowhere leaves the grid's edit around the field too, as a blur does.
        var root = _control().Closest("[data-tb-focus-root]");
        if (root?.GetAttribute("data-tb-focus-root") == "field" && root.HasAttribute("blazor:ontbfocusleave"))
        {
            await root.TriggerEventAsync("ontbfocusleave",
                new FieldFocusEventArgs { CrossesField = true, CrossesEdit = true });
        }
    }

    // A calendar's driver holds its whole box, and the part users act on inside it carries the description.
    private static IElement Described(IElement control) => control.HasAttribute("aria-describedby") ? control
        : control.QuerySelector("[aria-describedby]") ?? control;
}

/// <summary>The Trailblazor components in a rendered component's markup. Each helper finds its component again on every
/// call, so it sees the markup as it is after the last render.</summary>
public sealed class TrailblazorMarkup
{
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TrailblazorMarkup(Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts) =>
        (_findAll, _scripts) = (findAll, scripts);

    /// <summary>The Trailblazor components inside the element <paramref name="scope"/> finds, such as a grid's row,
    /// so a field that repeats in every row can be told apart:
    /// <c>cut.Tb().Within(() =&gt; cut.Tb().Grid("Orders").Row("Tent")).Select("Size")</c>. The element is found again
    /// each time a helper looks for a component, since one found before a render is stale after it.</summary>
    public TrailblazorMarkup Within(Func<IElement> scope) =>
        new(selector => scope().QuerySelectorAll(selector).ToList(), _scripts);

    /// <summary>The fields of the <c>TbPropertyGrid</c> with this label, found by their labels as anywhere else:
    /// <c>cut.Tb().PropertyGrid("Trail properties").NumberField("Length (km)")</c>.</summary>
    public TrailblazorMarkup PropertyGrid(string label) => Within(() =>
    {
        var grids = _findAll(".tb-property-grid[role=group]").Where(grid => grid.GetAttribute("aria-label") == label)
            .ToList();
        return grids.Count == 1 ? grids[0]
            : throw new InvalidOperationException($"Expected one property grid labeled \"{label}\", found {grids.Count}.");
    });

    /// <summary>The <c>TbViewport</c> whose box has this <c>Label</c>, or the only one in the markup when no label is
    /// given.</summary>
    public TbViewportDriver Viewport(string? label = null) => new(() =>
    {
        var viewports = _findAll("[data-part=viewport]").Where(viewport => label is null
            || viewport.QuerySelector(":scope > [data-part=surface]")?.GetAttribute("aria-label") == label).ToList();
        return viewports.Count == 1 ? viewports[0]
            : throw new InvalidOperationException(label is null
                ? $"Expected one viewport, found {viewports.Count}; name the one to use."
                : $"Expected one viewport labeled \"{label}\", found {viewports.Count}.");
    }, _findAll);

    /// <summary>The <c>TbQueryBuilder</c> with this <c>Label</c>, or the only one in the markup when no label is
    /// given.</summary>
    public TbQueryBuilderDriver QueryBuilder(string? label = null) => new(() =>
    {
        var builders = _findAll("[data-part=query-builder]")
            .Where(builder => label is null || builder.GetAttribute("aria-label") == label).ToList();
        return builders.Count == 1 ? builders[0]
            : throw new InvalidOperationException(label is null
                ? $"Expected one query builder, found {builders.Count}; name the one to use."
                : $"Expected one query builder labeled \"{label}\", found {builders.Count}.");
    }, _findAll, _scripts);

    /// <summary>The <c>TbDockManager</c> with this <c>AriaLabel</c>, or the only one in the markup when no name is
    /// given.</summary>
    public TbDockDriver Dock(string? name = null) => new(() =>
    {
        var docks = _findAll("[data-part=dock]").Where(dock => name is null || dock.GetAttribute("aria-label") == name)
            .ToList();
        return docks.Count == 1 ? docks[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one dock, found {docks.Count}; name the one to use."
                : $"Expected one dock named \"{name}\", found {docks.Count}.");
    }, _findAll, _scripts);

    /// <summary>The <c>TbTextField</c> or <c>TbCodeField</c> with this label, or with this <c>AriaLabel</c> when it
    /// has no label.</summary>
    public TbTextFieldDriver TextField(string label) => new(() => Field(label, ".tb-text-field input[data-part=input]",
        ".tb-text-field textarea[data-part=input]", "input.tb-code-field__input"));

    /// <summary>The <c>TbSelect</c> or <c>TbMultiSelect</c> with this label, or with this <c>AriaLabel</c> when it
    /// has no label.</summary>
    public TbSelectDriver Select(string label) =>
        new(() => Field(label, "[role=combobox]", "select"), _findAll, _scripts);

    /// <summary>The <c>TbListBox</c> or <c>TbMultiListBox</c> with this label, or with this <c>AriaLabel</c> when it
    /// has no label.</summary>
    public TbListBoxDriver ListBox(string label) => new(() => Field(label, "[role=listbox]", "select"));

    /// <summary>The <c>TbTransferList</c> with this label.</summary>
    public TbTransferListDriver TransferList(string label) => new(() =>
    {
        var lists = _findAll("fieldset.tb-transfer-list").Where(list =>
            list.QuerySelector("legend[data-part=label]") is { } legend && LabelText(legend) == label).ToList();
        return lists.Count == 1 ? lists[0] : throw new InvalidOperationException(lists.Count == 0
            ? $"No transfer list is labeled \"{label}\"." : $"{lists.Count} transfer lists are labeled \"{label}\".");
    });

    /// <summary>The <c>TbCheckbox</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbCheckboxDriver Checkbox(string label) =>
        new(() => Field(label, "input[type=checkbox][data-part=input]:not([role=switch])"));

    /// <summary>The <c>TbSwitch</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbCheckboxDriver Switch(string label) => new(() => Field(label, "input[type=checkbox][role=switch]"));

    /// <summary>The <c>TbCheckboxGroup</c> with this label.</summary>
    public TbCheckboxGroupDriver CheckboxGroup(string label) => new(() => Group(label, "fieldset.tb-checkbox-group"));

    /// <summary>The <c>TbRadioGroup</c> with this label.</summary>
    public TbRadioGroupDriver RadioGroup(string label) => new(() => Group(label, "fieldset.tb-radio-group"));

    /// <summary>The <c>TbRating</c> with this label.</summary>
    public TbRatingDriver Rating(string label) => new(() => Group(label, "fieldset.tb-rating"));

    /// <summary>The <c>TbToggleGroup</c> or <c>TbMultiToggleGroup</c> with this label, or with this
    /// <c>AriaLabel</c> when it has no label.</summary>
    public TbToggleGroupDriver ToggleGroup(string label) => new(() =>
        _findAll(".tb-toggle-group[role=group]").Where(group => (group.GetAttribute("aria-label")
                ?? _findAll($"[id='{group.GetAttribute("aria-labelledby")}']").SingleOrDefault()?.TextContent.Trim())
            == label).ToList() is [var group] ? group
            : throw new InvalidOperationException($"Expected one toggle group named \"{label}\"."));

    /// <summary>The <c>TbOverlay</c> with this <c>aria-label</c>, or the only one in the markup when no name is
    /// given. A closed overlay renders nothing, so it is found only while open.</summary>
    public TbOverlayDriver Overlay(string? name = null) => new(() =>
        _findAll(".tb-overlay").Where(overlay => name is null || overlay.GetAttribute("aria-label") == name).ToList(),
        name, _scripts);

    /// <summary>The <c>TbList</c> with this <c>aria-label</c>, or the only one in the markup when no name is given.
    /// </summary>
    public TbListDriver List(string? name = null) => new(() =>
    {
        var all = _findAll(".tb-list[role=list]");
        var lists = all.Where(list => name is null || list.GetAttribute("aria-label") == name).ToList();
        if (lists.Count == 0 && all.Count > 0)
        {
            throw new InvalidOperationException($"No list is named \"{name}\". The lists are named: "
                + string.Join(", ", all.Select(list => list.GetAttribute("aria-label") is { } label ? $"\"{label}\""
                    : "one without a name")) + ".");
        }

        return Only(lists, "list", name);
    }, _scripts);

    /// <summary>The <c>TbNumberField</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbNumberFieldDriver NumberField(string label) =>
        new(() => Field(label, "input[type=number]", "input[role=spinbutton]"));

    /// <summary>The <c>TbSlider</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbSliderDriver Slider(string label) => new(() => Field(label, "input[type=range][data-part=input]"));

    /// <summary>The <c>TbSplitter</c> separator with this name, such as "Resize Trails".</summary>
    public TbSplitterDriver Splitter(string separator) => new(() =>
    {
        var found = _findAll(".tb-splitter__separator")
            .Where(element => element.GetAttribute("aria-label") == separator).ToList();
        return found.Count == 1 ? found[0] : throw new InvalidOperationException(found.Count == 0
            ? $"No separator is named \"{separator}\"." : $"{found.Count} separators are named \"{separator}\".");
    }, _scripts);

    /// <summary>The <c>TbRangeSlider</c> with this label.</summary>
    public TbRangeSliderDriver RangeSlider(string label) =>
        new(() => Field(label, "input[type=range][data-part=start-thumb]"));

    /// <summary>The <c>TbAutocomplete</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbAutocompleteDriver Autocomplete(string label) =>
        new(() => Field(label, ".tb-autocomplete input[data-part=input]"), _findAll, _scripts);

    /// <summary>The <c>TbMentionInput</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbMentionInputDriver MentionInput(string label) =>
        new(() => Field(label, ".tb-mention-input__input"), _findAll, _scripts);

    /// <summary>The <c>TbCascader</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbCascaderDriver Cascader(string label) =>
        new(() => Field(label, ".tb-cascader input[data-part=input]"), _findAll);

    /// <summary>The <c>TbColorPicker</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbColorPickerDriver ColorPicker(string label) =>
        new(() => Field(label, ".tb-color-picker input[data-part=input]"));

    /// <summary>The <c>TbFileUpload</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbFileUploadDriver FileUpload(string label) =>
        new(() => Field(label, "input[type=file][data-part=input]"), _scripts);

    /// <summary>The open dialog with this title, or the only open dialog when no title is given: a <c>TbDialog</c> or
    /// <c>TbDrawer</c>, or one from <c>IDialogService</c> such as a confirm. A dialog from the service renders in the
    /// <c>DialogHost</c> that <c>TbRoot</c> holds, so look for it in the markup of the component that renders the
    /// host.</summary>
    public TbDialogDriver Dialog(string? title = null) => new(title, _findAll, _scripts);

    /// <summary>The open <c>TbLightbox</c> with this title, or the only open one when no title is given.</summary>
    public TbLightboxDriver Lightbox(string? title = null) => new(Dialog(title));

    /// <summary>The <c>TbTagInput</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbTagInputDriver TagInput(string label) =>
        new(() => Field(label, "input[data-part=input].tb-tag-input__input"));

    /// <summary>The <c>TbCalendar</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbCalendarDriver Calendar(string label) => new(() => Field(label, ".tb-calendar"));

    /// <summary>The <c>TbSignaturePad</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbSignaturePadDriver SignaturePad(string label) => new(() =>
        _findAll(".tb-signature-pad").Where(pad => pad.QuerySelector(":scope > [data-part=label]") is { } named
            && LabelText(named) == label).ToList() is [var pad]
            ? pad : throw new InvalidOperationException($"No signature pad is labeled \"{label}\"."));

    /// <summary>The <c>TbRichTextEditor</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbRichTextEditorDriver RichTextEditor(string label) =>
        new(() => Field(label, "[role=textbox][contenteditable]"), _findAll, _scripts);

    /// <summary>The <c>TbTour</c> on the page; a page shows one tour at a time.</summary>
    public TbTourDriver Tour() => new(() => _findAll(".tb-tour"));

    /// <summary>The <c>TbMarkdownEditor</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbMarkdownEditorDriver MarkdownEditor(string label) =>
        new(() => Field(label, "textarea.tb-markdown-editor__input"));

    /// <summary>The <c>TbTimePicker</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbTimePickerDriver TimePicker(string label) => new(() => Field(label, "input[type=time][data-part=input]"));

    /// <summary>The <c>TbDatePicker</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbDatePickerDriver DatePicker(string label) =>
        new(() => Field(label, "input[data-format]", "input[type=date][data-static]:not(.tb-calendar__input)"));

    /// <summary>The <c>TbDateTimePicker</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbDateTimePickerDriver DateTimePicker(string label) =>
        new(() => Field(label, "[role=group][data-format]:has([data-part=time-input])",
            "input[type=datetime-local][data-static]"));

    /// <summary>The <c>TbDateRangePicker</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbDateRangePickerDriver DateRangePicker(string label) =>
        new(() => Field(label, "[role=group][data-format]:has([data-part=end-input])"));

    /// <summary>The <c>TbPagination</c> with this accessible name, or the only one in the markup when no name is
    /// given.</summary>
    public TbPaginationDriver Pagination(string? name = null) => new(() =>
    {
        var navs = _findAll("nav[data-page-count]")
            .Where(nav => name is null || nav.GetAttribute("aria-label") == name).ToList();
        return navs.Count == 1 ? navs[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one pagination, found {navs.Count}; name the one to use."
                : $"No pagination is named \"{name}\".");
    });

    /// <summary>The <c>TbBreadcrumbs</c> with this accessible name, or the only one in the markup when no name is
    /// given.</summary>
    public TbBreadcrumbsDriver Breadcrumbs(string? name = null) => new(() =>
    {
        var trails = _findAll("nav.tb-breadcrumbs")
            .Where(nav => name is null || nav.GetAttribute("aria-label") == name).ToList();
        return trails.Count == 1 ? trails[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one breadcrumb trail, found {trails.Count}; name the one to use."
                : $"No breadcrumb trail is named \"{name}\".");
    }, _scripts);

    /// <summary>The <c>TbStepper</c> with this accessible name, or the only one in the markup when no name is
    /// given.</summary>
    public TbStepperDriver Stepper(string? name = null) => new(() =>
    {
        var steppers = _findAll("ol[data-part=stepper]")
            .Where(stepper => name is null || stepper.GetAttribute("aria-label") == name).ToList();
        return steppers.Count == 1 ? steppers[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one stepper, found {steppers.Count}; name the one to use."
                : $"No stepper is named \"{name}\".");
    });

    /// <summary>The <c>TbForm</c> with this accessible name, or the only one in the markup when no name is given.
    /// </summary>
    public TbFormDriver Form(string? name = null) => new(() =>
    {
        var forms = _findAll("form.tb-form").Where(form => name is null || name == (form.GetAttribute("aria-label")
            ?? _findAll($"[id='{form.GetAttribute("aria-labelledby")}']").SingleOrDefault()?.TextContent.Trim()))
            .ToList();
        return forms.Count == 1 ? forms[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one form, found {forms.Count}; name the one to use."
                : $"No form is named \"{name}\".");
    });

    /// <summary>The <c>TbAccordionItem</c>, in an accordion or on its own, whose header reads this: its full name, or,
    /// when the header shows a badge, any run of text in its title outside the badge, such as the title beside a
    /// count.</summary>
    public TbAccordionItemDriver AccordionItem(string header) => new(header, _findAll);

    /// <summary>The <c>TbAlert</c> whose text contains this, ignoring case and treating any run of whitespace as one
    /// space.</summary>
    public TbAlertDriver Alert(string text) => new(() =>
    {
        var alerts = _findAll(".tb-alert").Where(alert => Notices.Collapsed(alert.TextContent)
            .Contains(Notices.Collapsed(text), StringComparison.OrdinalIgnoreCase)).ToList();
        return alerts.Count == 1 ? alerts[0] : throw new InvalidOperationException(alerts.Count == 0
            ? $"No alert shows \"{text}\"." : $"{alerts.Count} alerts show \"{text}\".");
    });

    /// <summary>Every <c>TbAlert</c>'s message, in page order, as <c>Alert(text).Text</c> reads each.</summary>
    public IReadOnlyList<string> Alerts => [.. _findAll(".tb-alert").Select(Notices.Message)];

    /// <summary>The <c>TbBanner</c> with this accessible name (its <c>Title</c>, or its <c>AriaLabel</c> when it has
    /// no title), or the only one in the markup when no name is given.</summary>
    public TbBannerDriver Banner(string? name = null) => new(() => Only(_findAll("section.tb-banner")
        .Where(banner => name is null || (banner.GetAttribute("aria-labelledby") is { } id
            ? banner.QuerySelector($"[id='{id}']")?.TextContent.Trim() : banner.GetAttribute("aria-label")) == name)
        .ToList(), "banner", name));

    /// <summary>The <c>TbChat</c> with this <c>AriaLabel</c>, or the only one in the markup when no name is given.
    /// </summary>
    public TbChatDriver Chat(string? name = null) => new(() => Only(_findAll("section.tb-chat")
        .Where(chat => name is null || chat.GetAttribute("aria-label") == name).ToList(), "chat", name), _scripts);

    /// <summary>The <c>TbAiPrompt</c> with this <c>Label</c>, or the only one in the markup when no label is given.
    /// </summary>
    public TbAiPromptDriver AiPrompt(string? label = null) => new(() => Only(_findAll("section.tb-ai-prompt")
        .Where(prompt => label is null
            || prompt.QuerySelector(":scope > [data-part=composer] > [data-part=label]")!.TextContent.Trim() == label)
        .ToList(), "AI prompt", label));

    /// <summary>The <c>TbCarousel</c> with this <c>AriaLabel</c>, or the only one in the markup when no name is
    /// given.</summary>
    public TbCarouselDriver Carousel(string? name = null) => new(() => Only(_findAll("section.tb-carousel")
        .Where(carousel => name is null || carousel.GetAttribute("aria-label") == name).ToList(), "carousel", name));

    /// <summary>The <c>TbVirtualList</c> with this <c>AriaLabel</c>, or the only one in the markup when no name is
    /// given.</summary>
    public TbVirtualListDriver VirtualList(string? name = null) => new(() =>
    {
        var lists = _findAll(".tb-virtual-list[data-part=feed]")
            .Where(list => name is null || list.GetAttribute("aria-label") == name).ToList();
        return lists.Count == 1 ? lists[0]
            : throw new InvalidOperationException(name is not null
                ? lists.Count == 0 ? $"No virtual list is named \"{name}\"."
                    : $"Expected one virtual list named \"{name}\", found {lists.Count}."
                : lists.Count == 0 ? "No virtual list is in the markup."
                : $"Expected one virtual list, found {lists.Count}; name the one to use.");
    });

    /// <summary>The <c>TbChip</c> whose text is this.</summary>
    public TbChipDriver Chip(string text) => new(() => ChipParts.Find(_findAll(".tb-chip"), text), _scripts);

    /// <summary>The <c>TbChipGroup</c> with this <c>AriaLabel</c>, or the only one in the markup when no name is
    /// given.</summary>
    public TbChipGroupDriver ChipGroup(string? name = null) => new(() => Only(_findAll(".tb-chip-group")
        .Where(group => name is null || group.GetAttribute("aria-label") == name).ToList(), "chip group", name),
        _scripts);

    /// <summary>The <c>TbButton</c> or <c>TbIconButton</c> with this accessible name, a button or a link.</summary>
    public TbButtonDriver Button(string name) => new(() =>
    {
        var all = _findAll("button.tb-button, a.tb-button");
        var buttons = all.Where(button => Choices.NameOf(button) == name).ToList();
        return buttons.Count == 1 ? buttons[0] : throw new InvalidOperationException(buttons.Count == 0
            ? $"No button is named \"{name}\".{Choices.Present("buttons", "named", all.Select(Choices.NameOf))}"
            : $"{buttons.Count} buttons are named \"{name}\".");
    }, _scripts);

    /// <summary>The <c>TbLink</c> with this accessible name, a link or, without an address, a button.</summary>
    public TbLinkDriver Link(string name) => new(() =>
    {
        var all = _findAll("a.tb-link, button.tb-link");
        var links = all.Where(link => Choices.NameOf(link) == name).ToList();
        return links.Count == 1 ? links[0] : throw new InvalidOperationException(links.Count == 0
            ? $"No link is named \"{name}\".{Choices.Present("links", "named", all.Select(Choices.NameOf))}"
            : $"{links.Count} links are named \"{name}\".");
    }, _scripts);

    /// <summary>The <c>TbNavMenu</c> in the navigation landmark with this name, or the only one in the markup when no
    /// name is given. While a <c>TbAppShell</c>'s drawer is open, the menu in the drawer is the one found.</summary>
    public TbNavMenuDriver NavMenu(string? name = null) => new(() =>
    {
        var menus = _findAll("ul.tb-nav-menu").Where(menu => name is null
            || menu.Closest("nav, [role=navigation]") is { } landmark && name == (landmark.GetAttribute("aria-label")
                ?? _findAll($"[id='{landmark.GetAttribute("aria-labelledby")}']").SingleOrDefault()?.TextContent
                    .Trim())).ToList();
        // An app shell's open drawer holds a second copy of its menu, the one a user sees on a narrow window.
        var drawn = menus.Where(menu => menu.Closest("dialog[data-part=dialog]") is not null).ToList();
        menus = drawn.Count == 1 ? drawn : menus;
        return menus.Count == 1 ? menus[0] : throw new InvalidOperationException(name is null
            ? $"Expected one nav menu, found {menus.Count}; name the one to use by its navigation landmark."
            : $"Expected one nav menu in a navigation landmark named \"{name}\", found {menus.Count}.");
    }, _scripts);

    /// <summary>The <c>TbSpeechToText</c> whose button has this <c>Label</c>, such as "Dictate".</summary>
    public TbSpeechToTextDriver SpeechToText(string label) => new(() =>
    {
        var buttons = _findAll(".tb-speech-to-text [data-part=button]")
            .Where(button => button.GetAttribute("aria-label") == label).ToList();
        return buttons.Count == 1 ? buttons[0] : throw new InvalidOperationException(buttons.Count == 0
            ? $"No dictation button is labeled \"{label}\"."
            : $"{buttons.Count} dictation buttons are labeled \"{label}\".");
    }, _scripts);

    /// <summary>The <c>TbText</c> with <c>ShowFullText</c> or <c>MaxLength</c> whose whole text is this.</summary>
    public TbTextDriver Text(string text) => new(() =>
    {
        var texts = _findAll(".tb-text").Where(element => TbTextDriver.FullTextOf(element) == text).ToList();
        return texts.Count == 1 ? texts[0] : throw new InvalidOperationException(texts.Count == 0
            ? $"No text with ShowFullText or MaxLength reads \"{text}\"."
            : $"{texts.Count} texts read \"{text}\".");
    }, _scripts);

    /// <summary>The <c>TbSmartPaste</c> whose button has this <c>Label</c>, or the only one when no label is given.
    /// </summary>
    public TbSmartPasteDriver SmartPaste(string? label = null) => new(() => Only(_findAll(".tb-smart-paste")
        .Where(paste => label is null
            || paste.QuerySelector("[data-part=button] [data-part=label]")!.TextContent.Trim() == label)
        .ToList(), "smart paste button", label), _findAll);

    /// <summary>The only <c>TbDescriptionList</c> in the scope, leaving out one inside another's value;
    /// <see cref="Within"/> tells several apart.</summary>
    public TbDescriptionListDriver DescriptionList() => new(() =>
    {
        var lists = _findAll(".tb-description-list");
        return Only([.. lists.Where(list => !lists.Any(outer => outer != list && outer.Contains(list)))],
            "description list", null);
    });

    /// <summary>The <c>TbValidationSummary</c> in the scope, which shows only while its form has messages.</summary>
    public TbValidationSummaryDriver ValidationSummary() => new(() => _findAll(".tb-validation-summary"));

    /// <summary>The <c>TbCopyButton</c> with these words, its <c>Labels.Copy</c>, such as "Copy", or the only one
    /// when none are given.</summary>
    public TbCopyButtonDriver CopyButton(string? label = null) => new(() => Only(_findAll(".tb-copy-button")
        .Where(button => label is null || button.GetAttribute("data-label") == label).ToList(), "copy button", label));

    /// <summary>The <c>TbAsyncContent</c> with this <c>Label</c>, such as "groups", or the only one when no label is
    /// given.</summary>
    public TbAsyncContentDriver AsyncContent(string? label = null) => new(() => Only(_findAll(".tb-async-content")
        .Where(region => label is null || region.GetAttribute("data-label") == label).ToList(),
        "async content region", label), _scripts);

    /// <summary>The <c>TbInPlaceEditor</c> with this <c>Label</c>.</summary>
    public TbInPlaceEditorDriver InPlaceEditor(string label) => new(() =>
    {
        var editors = _findAll(".tb-in-place-editor").Where(editor =>
            editor.QuerySelector("[data-part=label]")?.TextContent == label).ToList();
        return editors.Count == 1 ? editors[0] : throw new InvalidOperationException(editors.Count == 0
            ? $"No in-place editor is labeled \"{label}\"."
            : $"{editors.Count} in-place editors are labeled \"{label}\".");
    });

    /// <summary>The page's <c>TbBackToTop</c>.</summary>
    public TbBackToTopDriver BackToTop() => new(() =>
    {
        var buttons = _findAll(".tb-back-to-top").ToList();
        return buttons.Count == 1 ? buttons[0] : throw new InvalidOperationException(buttons.Count == 0
            ? "No back to top button is on the page."
            : $"{buttons.Count} back to top buttons are on the page.");
    });

    /// <summary>The <c>TbInsertDivider</c> whose insert button has this <c>Label</c>.</summary>
    public TbInsertDividerDriver InsertDivider(string label) => new(() =>
    {
        var dividers = _findAll(".tb-insert-divider").Where(divider =>
            divider.QuerySelector("[data-part=insert]")?.GetAttribute("aria-label") == label).ToList();
        return dividers.Count == 1 ? dividers[0] : throw new InvalidOperationException(dividers.Count == 0
            ? $"No insert divider is labeled \"{label}\"."
            : $"{dividers.Count} insert dividers are labeled \"{label}\".");
    });

    /// <summary>The <c>TbToolbar</c> with this <c>AriaLabel</c>, or the only one in the markup when no name is
    /// given.</summary>
    public TbToolbarDriver Toolbar(string? name = null) => new(() => Only(_findAll(".tb-toolbar[role=toolbar]")
        .Where(toolbar => name is null || toolbar.GetAttribute("aria-label") == name).ToList(), "toolbar", name));

    /// <summary>The <c>TbRibbon</c> with this <c>AriaLabel</c>, or the only one in the markup when no name is
    /// given.</summary>
    public TbRibbonDriver Ribbon(string? name = null) => new(() => Only(_findAll(".tb-ribbon")
        .Where(ribbon => name is null
            || ribbon.QuerySelector(":scope > .tb-tabs > [role=tablist]")?.GetAttribute("aria-label") == name)
        .ToList(), "ribbon", name), _findAll, _scripts);

    private static IElement Only(List<IElement> found, string kind, string? name) => found.Count switch
    {
        1 => found[0],
        0 => throw new InvalidOperationException(name is null ? $"No {kind} is in the markup."
            : $"No {kind} is named \"{name}\"."),
        _ => throw new InvalidOperationException(name is null
            ? $"Expected one {kind}, found {found.Count}; name the one to use."
            : $"{found.Count} {kind}s are named \"{name}\"."),
    };

    /// <summary>The <c>TbTooltip</c> around the button or link with this accessible name.</summary>
    public TbTooltipDriver Tooltip(string trigger) => new(() =>
    {
        var controls = _findAll(".tb-tooltip-anchor button, .tb-tooltip-anchor a[href]")
            .Where(control => Choices.NameOf(control) == trigger).ToList();
        return controls.Count == 1 ? controls[0].Closest(".tb-tooltip-anchor")! : throw new InvalidOperationException(
            controls.Count == 0 ? $"No tooltip's trigger is named \"{trigger}\"."
                : $"{controls.Count} tooltips' triggers are named \"{trigger}\".");
    });

    /// <summary>The <c>TbPopconfirm</c> whose trigger has this name.</summary>
    public TbPopconfirmDriver Popconfirm(string trigger) => new(trigger, _findAll);

    /// <summary>The toasts from <c>IToastService</c>, which the rendered <c>TbRoot</c> shows.</summary>
    public TbToastsDriver Toasts => new(_findAll, _scripts);
    /// <summary>The <c>TbPopover</c> whose trigger has this name.</summary>
    public TbPopoverDriver Popover(string trigger) => new(trigger, _findAll, _scripts);

    /// <summary>The <c>TbMenu</c> whose trigger has this name.</summary>
    public TbMenuDriver Menu(string trigger) => new(trigger, _findAll, _scripts);

    /// <summary>The <c>TbMenubar</c> with this <c>AriaLabel</c>, or the only one in the markup when no name is given.
    /// </summary>
    public TbMenubarDriver Menubar(string? name = null) => new(() => Only(_findAll("[role=menubar].tb-menubar")
        .Where(bar => name is null || bar.GetAttribute("aria-label") == name).ToList(), "menubar", name), _scripts);

    /// <summary>The <c>TbHotkey</c> with these keys, written as its <c>Keys</c> are, such as <c>"Mod+S"</c>.</summary>
    public TbHotkeyDriver Hotkey(string keys) => new(() =>
    {
        var hotkeys = _findAll("[data-part=hotkey]").Where(hotkey => hotkey.GetAttribute("data-keys") == keys).ToList();
        return hotkeys.Count == 1 ? hotkeys[0]
            : throw new InvalidOperationException($"Expected one hotkey with the keys \"{keys}\", found "
                + $"{hotkeys.Count}; write the keys as its Keys are, or use Within when several match.");
    });

    /// <summary>The <c>TbContextMenu</c> whose menu has this <c>AriaLabel</c>, or the only one in the markup when no
    /// name is given.</summary>
    public TbContextMenuDriver ContextMenu(string? name = null) => new(() =>
    {
        var targets = _findAll(".tb-context-menu[data-part=target]")
            .Where(target => name is null || TbContextMenuDriver.Menu(target).GetAttribute("aria-label") == name)
            .ToList();
        return targets.Count == 1 ? targets[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one context menu, found {targets.Count}; name the one to use."
                : $"Expected one context menu named \"{name}\", found {targets.Count}.");
    }, _scripts);

    /// <summary>The <c>TbTabs</c> with this accessible name, or the only tabs in the markup when no name is given.
    /// </summary>
    public TbTabsDriver Tabs(string? name = null) => new(() =>
    {
        // With no name, only outer tab lists count, so tabs nested in a panel don't make the lookup ambiguous. A
        // panel outside the scope doesn't count, so the helpers scoped to a panel find the tabs in it.
        var panels = _findAll("[role=tabpanel]");
        var lists = _findAll("[role=tablist]").Where(list => name is null
            ? !panels.Any(panel => panel.Contains(list)) : list.GetAttribute("aria-label") == name).ToList();
        return lists.Count == 1 ? lists[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one tab list, found {lists.Count}; name the one to use."
                : $"No tab list is named \"{name}\".");
    }, _findAll, _scripts);

    /// <summary>The <c>TbTreeSelect</c> or <c>TbMultiTreeSelect</c> with this label, or with this <c>AriaLabel</c>
    /// when it has no label.</summary>
    public TbTreeSelectDriver TreeSelect(string label) =>
        new(() => Field(label, ".tb-tree-select__trigger"), _findAll);

    /// <summary>The <c>TbTreeView</c> with this accessible name, or the only tree in the markup when no name is given.
    /// </summary>
    public TbTreeDriver Tree(string? name = null) => new(() =>
    {
        var trees = _findAll(".tb-tree").Where(tree => name is null || tree.GetAttribute("aria-label") == name)
            .ToList();
        return trees.Count == 1 ? trees[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one tree, found {trees.Count}; name the one to use."
                : $"No tree is named \"{name}\".");
    });

    /// <summary>The <c>TbDataGrid</c> whose table has this accessible name, or the only grid in the markup when no
    /// name is given.</summary>
    public TbGridDriver Grid(string? name = null) => new(_findAll, () =>
    {
        var tables = _findAll("[data-part=grid] table")
            .Where(table => name is null || table.GetAttribute("aria-label") == name).ToList();
        return tables.Count == 1 ? tables[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one data grid, found {tables.Count}; name the one to use."
                : $"No data grid is named \"{name}\".");
    }, _scripts);

    /// <summary>The <c>TbCommandPalette</c> with this label, or the only one in the markup when no label is given.
    /// </summary>
    public TbCommandPaletteDriver CommandPalette(string? name = null) => new(() =>
    {
        var markers = _findAll("[data-command-palette]")
            .Where(marker => name is null || marker.GetAttribute("data-command-palette") == name).ToList();
        return markers.Count == 1 ? markers[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one command palette, found {markers.Count}; name the one to use."
                : $"No command palette is named \"{name}\".");
    }, _findAll);

    /// <summary>The <c>TbKanban</c> with this <c>AriaLabel</c>, or the only one in the markup when no name is given.
    /// </summary>
    public TbKanbanDriver Kanban(string? name = null) => new(() => Only(_findAll("section.tb-kanban")
        .Where(board => name is null || board.GetAttribute("aria-label") == name).ToList(), "Kanban board", name),
        _findAll, _scripts);

    /// <summary>The <c>TbSortable</c> with this label, or the only one in the markup when no label is given.
    /// </summary>
    public TbSortableDriver Sortable(string? name = null) => new(() =>
    {
        var lists = _findAll("[data-part=list][data-sortable]")
            .Where(list => name is null || list.GetAttribute("aria-label") == name).ToList();
        return lists.Count == 1 ? lists[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one sortable list, found {lists.Count}; name the one to use."
                : $"No sortable list is named \"{name}\".");
    }, _findAll, _scripts);

    // A field's label names its control by for or aria-labelledby; a required field's label ends with an asterisk. A
    // field with no label, such as one editing a grid's cell, is named by its aria-label, or by any one of the
    // elements its own aria-labelledby names, as Playwright's GetByLabel finds it too.
    private IElement Field(string label, params string[] controls)
    {
        // A label for an id names that element, as in a browser, such as a TbLabel's beside its field.
        var all = _findAll("label[data-part=label], label[for]");
        var labels = all.Where(element => LabelText(element) == label).ToList();
        var candidates = _findAll(string.Join(", ", controls));
        var unlabeled = labels.Count > 0 ? [] : candidates.Where(control =>
            control.GetAttribute("aria-label") == label || Choices.Labels(control).Contains(label)).ToList();
        if (unlabeled is [var found])
        {
            return found;
        }

        if (labels.Count == 0)
        {
            throw new InvalidOperationException(unlabeled.Count > 1
                ? $"{unlabeled.Count} fields are labeled \"{label}\"."
                : $"No field is labeled \"{label}\"." + Choices.Present("fields", "labeled", all.Select(LabelText)
                    .Concat(candidates.SelectMany(control =>
                        Choices.Labels(control).Prepend(control.GetAttribute("aria-label") ?? "")))));
        }

        var named = labels.Select(ControlOf).OfType<IElement>().ToList();
        if (named.Count == 0)
        {
            throw new InvalidOperationException($"The label \"{label}\" names no control a browser labels. A label's "
                + "for names only an input, select, textarea or button; name another control, such as a combobox, "
                + "with aria-labelledby and the label's id.");
        }

        // A slider or checkbox can share the label, so only the fields of the kind asked for count.
        var fields = named.Where(control => controls.Any(control.Matches)).ToList();
        return fields.Count switch
        {
            1 => fields[0],
            0 => throw new InvalidOperationException(labels.Count == 1
                ? $"The field labeled \"{label}\" isn't the kind asked for."
                : $"None of the {labels.Count} fields labeled \"{label}\" is the kind asked for."),
            _ => throw new InvalidOperationException($"{fields.Count} fields are labeled \"{label}\"."),
        };
    }

    // A checkbox's label holds its input. A label's for names only what a browser labels, not a div with a role.
    private IElement? ControlOf(IElement label) => label.GetAttribute("for") is { Length: > 0 } target
        ? _findAll($"[id='{target}']").FirstOrDefault(element => element.Matches(Labelable))
        : _findAll($"[aria-labelledby~='{label.Id}']").FirstOrDefault() ?? label.QuerySelector("input");

    // A group is named by the text in its legend, not the info button beside it.
    private IElement Group(string label, string selector)
    {
        var groups = _findAll(selector).Where(group =>
            group.QuerySelector(":scope > legend[data-part=label] > span") is { } name && LabelText(name) == label)
            .ToList();
        return groups.Count == 1 ? groups[0] : throw new InvalidOperationException(groups.Count == 0
            ? $"No group is labeled \"{label}\"." : $"{groups.Count} groups are labeled \"{label}\".");
    }

    private const string Labelable = "input:not([type=hidden]), select, textarea, button, meter, output, progress";

    private static string LabelText(IElement label) => label.TextContent.Trim().TrimEnd('*').Trim();
}

/// <summary>A <c>TbSelect</c> or <c>TbMultiSelect</c>, found by its label.</summary>
public sealed class TbSelectDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbSelectDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts)
        : base(find) => (_find, _findAll, _scripts) = (find, findAll, scripts);

    /// <summary>The options' names, in order, leaving out a static select's placeholder, the extra option it adds to
    /// keep a value that matches no option, and the message an empty list shows. An option with content of its own is
    /// named by its <c>Text</c>.</summary>
    public IReadOnlyList<string> Options => OptionElements().Select(Choices.NameOf).ToList();

    /// <summary>The option with this name, and whether it's disabled and chosen. Throws if no option or more than one
    /// option has this name.</summary>
    public OptionInfo Option(string name) => Choices.Describe(OptionElements(), name);

    /// <summary>Whether the select is disabled, so its options can't be chosen.</summary>
    public bool Disabled => Choices.IsDisabled(_find());

    /// <summary>The name of the select's value, as a screen reader hears it, or null while it shows its placeholder. A
    /// multiple select's chosen options are joined with ", ".</summary>
    public string? Text
    {
        get
        {
            var control = _find();
            var shown = control.TagName == "SELECT" ? control.QuerySelectorAll("option[selected]:not([value=''])")
                : control.QuerySelectorAll("[data-part=value], [data-part=chip]");
            var texts = shown.Select(Choices.NameOf).ToList();
            return texts.Count > 0 ? string.Join(", ", texts) : null;
        }
    }

    /// <summary>Chooses the options with these names, as clicking them does: the one option of a select, or each one
    /// not yet chosen in a multiple select, keeping what is already chosen. On a static page the select is a native
    /// one that posts with its form, so this throws, as it does for a disabled or read-only select. As in a browser,
    /// the list is opened before each option if it's closed, and closed once they're chosen, so the app's
    /// <c>OpenChanged</c> runs.</summary>
    public async Task ChooseAsync(params string[] options) => await SetChosenAsync(options, choose: true);

    /// <summary>The names of the chosen options, in order. A list of more than 100 options shows only those in view,
    /// so for one that long this throws; read <see cref="Text"/> instead.</summary>
    public IReadOnlyList<string> Chosen
    {
        get
        {
            var control = _find();
            if (_findAll($"[id='{control.GetAttribute("aria-controls")}'][data-count], "
                    + $"[id='{control.GetAttribute("aria-controls")}'] [data-count]").Count > 0)
            {
                throw new InvalidOperationException(
                    "The list shows only the options in view, so their names can't all be read; read Text instead.");
            }

            // A multiple select's Select all row is an option, but it chooses the others rather than itself.
            return OptionElements().Where(option => !option.Matches("[data-part=select-all]")
                    && (option.GetAttribute("aria-selected") == "true" || option.HasAttribute("selected")))
                .Select(Choices.NameOf).ToList();
        }
    }

    /// <summary>Unchooses the options with these names in a multiple select, as clicking them again does, leaving the
    /// others chosen. A select that takes one option throws; clear it with <see cref="ClearAsync"/>. On a static page,
    /// or when the select is disabled or read-only, this throws too, as <see cref="ChooseAsync"/> does. Otherwise it
    /// opens and closes the list around the options the same way.</summary>
    public async Task UnchooseAsync(params string[] options) => await SetChosenAsync(options, choose: false);

    /// <summary>Whether the select's list is open.</summary>
    public bool IsOpen => _find().HasAttribute("data-open");

    /// <summary>Opens the select's list, as pressing Down on it does, and the app's <c>OpenChanged</c> runs. An open
    /// list is left as it is. A statically rendered, disabled or read-only select throws, since its list can't open.
    /// </summary>
    public async Task OpenAsync() => await SetOpenAsync(true);

    /// <summary>Closes the select's list, as pressing Escape does, and the app's <c>OpenChanged</c> runs. A closed list
    /// is left as it is.</summary>
    public async Task CloseAsync() => await SetOpenAsync(false);

    /// <summary>Clears the select with its clear button, as a user does. A select that isn't <c>Clearable</c>, or has
    /// nothing chosen, shows no clear button, so this throws, as it does on a static page or for a disabled select.
    /// </summary>
    public async Task ClearAsync()
    {
        var control = _find();
        Choices.ThrowIfStaticOrDisabled(control);
        var clear = control.Closest(".tb-field")?.QuerySelector("[data-part=clear]")
            ?? throw new InvalidOperationException(
                "The select shows no clear button; make it Clearable, and choose an option first.");
        await clear.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private async Task SetChosenAsync(string[] options, bool choose)
    {
        Choices.ThrowIfStaticOrDisabled(_find());
        await Choices.SetChosenAsync(Choosable, options, choose, OpenAsync);
        if (StillOpen())
        {
            await CloseAsync();
        }
    }

    private bool StillOpen()
    {
        try
        {
            return IsOpen;
        }
        catch (InvalidOperationException)
        {
            // The app can take the select away once it has the choice, as a grid's cell editor does.
            return false;
        }
    }

    private async Task SetOpenAsync(bool open)
    {
        var control = _find();
        if (open && control.TagName == "SELECT")
        {
            throw new InvalidOperationException("On a static page this field renders as a native select, which posts "
                + "with its form; render it on an interactive page to open its list.");
        }

        if (open && (Choices.IsDisabled(control) || control.GetAttribute("aria-readonly") == "true"))
        {
            throw new InvalidOperationException("This select is disabled or read-only, so its list can't open.");
        }

        if (control.HasAttribute("data-open") == open)
        {
            return;
        }

        // A select that attaches on first use does so when it gets focus.
        if (control.HasAttribute("data-attach-on-use"))
        {
            await control.FocusAsync(new FocusEventArgs());
        }

        var toggle = _scripts.Select(_find().Id);
        await _scripts.InvokeAsync(() => toggle.OnToggle(open));
    }

    private (IEnumerable<IElement> Options, bool Multiple) Choosable()
    {
        var control = _find();
        Choices.ThrowIfStaticOrDisabled(control);
        // A filterable select points to the dialog that holds its filter and listbox.
        var controls = control.GetAttribute("aria-controls");
        return (OptionElements(), _findAll($"[id='{controls}'][aria-multiselectable=true], "
            + $"[id='{controls}'] [aria-multiselectable=true]").Count > 0);
    }

    private IEnumerable<IElement> OptionElements()
    {
        var control = _find();
        return control.TagName == "SELECT"
            ? control.QuerySelectorAll("option:not([value='']):not([data-unmatched]):not([data-part=status])")
            : _findAll($"[id='{control.GetAttribute("aria-controls")}'] [role=option]:not([data-part=status])");
    }
}

/// <summary>A <c>TbTreeSelect</c> or <c>TbMultiTreeSelect</c>, found by its label. Items are named by their path of
/// texts from a root, such as <c>("North ridge", "Pine Hollow")</c>.</summary>
public sealed class TbTreeSelectDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbTreeSelectDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll) : base(find) =>
        (_find, _findAll) = (find, findAll);

    /// <summary>The text the field shows for its value, or null while it shows its placeholder.</summary>
    public string? Text => _find().QuerySelector("[data-part=value]") is { } value
        && !value.ClassList.Contains("tb-select__placeholder") ? value.TextContent.Trim() : null;

    /// <summary>Whether the tree select is disabled, so its tree can't open.</summary>
    public bool Disabled => Choices.IsDisabled(_find());

    /// <summary>Opens the rows above the item at this path and chooses it, as clicking it does.</summary>
    public async Task ChooseAsync(params string[] path) => await Tree().SelectAsync(path);

    /// <summary>Opens the rows above the item at this path, and reads its text and whether it's disabled and chosen,
    /// which in a multi tree select means checked. A disabled or read-only tree select throws, since it has no tree to
    /// read.</summary>
    public async Task<OptionInfo> OptionAsync(params string[] path)
    {
        var tree = Tree();
        await tree.ExpandAsync(path[..^1]);
        var row = tree.Item(path);
        return new(path[^1], Choices.IsDisabled(row), Choices.IsChosen(row));
    }

    /// <summary>Opens the rows above the item at this path and, unless it is checked already, checks it in a multi
    /// tree select as clicking its box does.</summary>
    public async Task CheckAsync(params string[] path) => await SetCheckedAsync(path, true);

    /// <summary>Opens the rows above the item at this path and, unless it is unchecked already, unchecks it in a
    /// multi tree select as clicking its box does.</summary>
    public async Task UncheckAsync(params string[] path) => await SetCheckedAsync(path, false);

    /// <summary>Types this text in the field's filter box, as the user does, so the tree shows what matches.</summary>
    public async Task FilterAsync(string text) => await (Panel().QuerySelector("[data-part=filter]")
        ?? throw new InvalidOperationException("This tree select has no filter box; set Filterable to add one."))
        .InputAsync(new ChangeEventArgs { Value = text });

    private async Task SetCheckedAsync(string[] path, bool check)
    {
        var tree = Tree();
        await tree.ExpandAsync(path[..^1]);
        // A partly checked row checks everything under it first, so unchecking it takes a second click.
        for (var clicks = 0; tree.Item(path).GetAttribute("aria-checked") != (check ? "true" : "false"); clicks++)
        {
            if (clicks == 2)
            {
                throw new InvalidOperationException($"The row at {string.Join(" > ", path)} can't be checked or "
                    + "unchecked.");
            }

            await tree.CheckAsync(path);
        }
    }

    // A disabled field's tree can't be used, and a read-only field has none.
    private IElement Panel()
    {
        var trigger = _find();
        return trigger.HasAttribute("disabled") || trigger.GetAttribute("aria-disabled") == "true" ? throw Closed()
            : _findAll($"[id='{trigger.GetAttribute("aria-controls")}']").SingleOrDefault() ?? throw Closed();

        static InvalidOperationException Closed() =>
            new("This tree select is disabled or read-only, so its tree can't be used.");
    }

    private TbTreeDriver Tree() => new(() => Panel().QuerySelector(".tb-tree")
        ?? throw new InvalidOperationException("This tree select's tree isn't there."));
}

/// <summary>A <c>TbListBox</c> or <c>TbMultiListBox</c>, found by its label.</summary>
public sealed class TbListBoxDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbListBoxDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The options' names, in order, leaving out the message an empty list shows. An option with content of
    /// its own is named by its <c>Text</c>.</summary>
    public IReadOnlyList<string> Options => OptionElements(_find()).Select(Choices.NameOf).ToList();

    /// <summary>The option with this name, and whether it's disabled and chosen. Throws if no option or more than one
    /// option has this name.</summary>
    public OptionInfo Option(string name) => Choices.Describe(OptionElements(_find()), name);

    /// <summary>Whether the list box is disabled, so its options can't be chosen.</summary>
    public bool Disabled => Choices.IsDisabled(_find());

    /// <summary>The names of the chosen options, in order.</summary>
    public IReadOnlyList<string> Chosen => _find()
        .QuerySelectorAll("[role=option][aria-selected=true], option[selected]")
        .Select(Choices.NameOf).ToList();

    /// <summary>Chooses the options with these names, as clicking them does: the one option of a list box, or each one
    /// not yet chosen in a multiple list box, keeping what is already chosen. On a static page, or when the list box is
    /// disabled, this throws, as for a select.</summary>
    public async Task ChooseAsync(params string[] options) =>
        await Choices.SetChosenAsync(Choosable, options, choose: true);

    /// <summary>Unchooses the options with these names in a multiple list box, as clicking them again does, leaving
    /// the others chosen. A list box that takes one option throws, since a click can't unchoose its option, and so
    /// does a static or disabled one, as for <see cref="ChooseAsync"/>.</summary>
    public async Task UnchooseAsync(params string[] options) =>
        await Choices.SetChosenAsync(Choosable, options, choose: false);

    /// <summary>Unchooses every chosen option in a multiple list box.</summary>
    public async Task ClearAsync()
    {
        if (Chosen.Count > 0)
        {
            await UnchooseAsync([.. Chosen]);
        }
    }

    private (IEnumerable<IElement> Options, bool Multiple) Choosable()
    {
        var list = _find();
        Choices.ThrowIfStaticOrDisabled(list);
        return (OptionElements(list), list.GetAttribute("aria-multiselectable") == "true");
    }

    private static IEnumerable<IElement> OptionElements(IElement list) =>
        list.QuerySelectorAll("[role=option]:not([data-part=status]), option:not([data-part=status])");
}

/// <summary>A <c>TbTransferList</c>, found by its label.</summary>
public sealed class TbTransferListDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbTransferListDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The chosen items' text, in order.</summary>
    public IReadOnlyList<string> Chosen => new TbListBoxDriver(() => List("chosen")).Options;

    /// <summary>Marks the available items with this text and moves them to the chosen list with its button.</summary>
    public async Task MoveAsync(params string[] items) => await MoveAsync("available", "move-chosen", items);

    /// <summary>Marks the chosen items with this text and moves them back to the available list with its button.
    /// </summary>
    public async Task MoveBackAsync(params string[] items) => await MoveAsync("chosen", "move-available", items);

    private async Task MoveAsync(string from, string button, string[] items)
    {
        await new TbListBoxDriver(() => List(from)).ChooseAsync(items);
        await _find().QuerySelector($"[data-part={button}]")!.ClickAsync(new MouseEventArgs());
    }

    private IElement List(string part) => _find().QuerySelector($"[data-part={part}] [role=listbox]")
        ?? throw new InvalidOperationException("The transfer list isn't interactive yet.");
}

// What the choice helpers share: picking options, reading names and refusing locked inputs, by the same rules as
// the Playwright helpers.
internal static class Choices
{
    // Clicks each named option whose state differs: one not yet chosen to choose it, or a chosen one to unchoose it.
    // A field whose list opens first, as a select's does, is opened before each option, so the app hears it open.
    internal static async Task SetChosenAsync(Func<(IEnumerable<IElement> Options, bool Multiple)> find,
        string[] texts, bool choose, Func<Task>? open = null)
    {
        ArgumentOutOfRangeException.ThrowIfZero(texts.Length, nameof(texts));
        if (choose && texts.Length > 1 && !find().Multiple)
        {
            throw new ArgumentException("A field that takes one option can't choose several.", nameof(texts));
        }

        if (!choose && !find().Multiple)
        {
            throw new InvalidOperationException("A field that takes one option can't unchoose it by clicking.");
        }

        foreach (var text in texts)
        {
            if (open is not null)
            {
                await open();
            }

            var option = Named(find().Options.ToList(), text);
            if (option.GetAttribute("aria-selected") == "true" != choose)
            {
                // A click from a pointer has a count, which the option reports as pointer input.
                await option.ClickAsync(new MouseEventArgs { Detail = 1 });
            }
        }
    }

    // An interactive field or option is turned off the ARIA way, and a static one natively.
    internal static bool IsDisabled(IElement element) =>
        element.GetAttribute("aria-disabled") == "true" || element.HasAttribute("disabled");

    internal static OptionInfo Describe(IEnumerable<IElement> options, string text)
    {
        var option = Named(options.ToList(), text);
        return new(text, IsDisabled(option), IsChosen(option));
    }

    // A tree select's item is chosen when selected, or in a multi tree select when checked.
    internal static bool IsChosen(IElement option) => option.GetAttribute("aria-selected") == "true"
        || option.HasAttribute("selected") || option.GetAttribute("aria-checked") == "true";

    // The one option with this name, or an error that lists the names there are.
    internal static IElement Named(List<IElement> options, string text) =>
        options.Where(option => NameOf(option) == text).ToList() switch
        {
            [var only] => only,
            [] => throw new InvalidOperationException(options.Count == 0
                ? $"No option reads \"{text}\": there are none."
                : $"No option reads \"{text}\". The options read: "
                    + string.Join(", ", options.Select(option => $"\"{NameOf(option)}\"")) + "."),
            var several => throw new InvalidOperationException(
                $"{several.Count} options read \"{text}\"; give each option a name of its own."),
        };

    // What a screen reader reads, as the Playwright helpers find it: the elements aria-labelledby names, else an
    // aria-label, else the text outside hidden parts.
    internal static string NameOf(IElement element) =>
        Labels(element) is { Count: > 0 } labels ? string.Join(' ', labels) : OwnName(element);

    // The names of the elements an aria-labelledby lists that are there, in its order. A control's name joins
    // them; a field is found by any one of them, as Playwright's GetByLabel finds it.
    internal static List<string> Labels(IElement element)
    {
        if (element.GetAttribute("aria-labelledby") is not { } ids)
        {
            return [];
        }

        // bUnit's markup isn't a document that knows its ids, so the named elements are looked for from its root,
        // and by comparing ids, since an id needn't be safe to put in a selector.
        var withIds = (element.GetRoot() as IParentNode)?.QuerySelectorAll("[id]").ToList() ?? [];
        return ids.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(id => withIds.FirstOrDefault(named => named.Id == id)).OfType<IElement>().Select(OwnName)
            .Where(name => name != "").ToList();
    }

    // A browser reads each run of spaces, tabs and line breaks as one space, so a template's line breaks and
    // indentation aren't part of the name. A non-breaking space is no such run, and stays as it is.
    private static string OwnName(IElement element) => element.GetAttribute("aria-label")
        ?? (element.GetAttribute("aria-hidden") == "true" ? ""
            : string.Join(' ', string.Concat(ContentName(element)).Trim().Split([' ', '\t', '\n', '\f', '\r'],
                StringSplitOptions.RemoveEmptyEntries)));

    // The text a name takes from what an element holds, in order, as a browser takes it: an element inside that has
    // an aria-label, such as an avatar, counts with that label, an image with its alternative text, and what is
    // aria-hidden or hidden doesn't count.
    private static IEnumerable<string> ContentName(INode node)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child is IText text)
            {
                yield return text.Data;
            }
            else if (child is not IElement inner || inner.GetAttribute("aria-hidden") == "true"
                || inner.HasAttribute("hidden"))
            {
                continue;
            }
            else if (inner.GetAttribute("aria-label") is { Length: > 0 } label)
            {
                yield return $" {label} ";
            }
            else if (inner.LocalName == "img")
            {
                yield return $" {inner.GetAttribute("alt")} ";
            }
            else
            {
                foreach (var part in ContentName(inner))
                {
                    yield return part;
                }
            }
        }
    }

    // What a failed lookup adds to its message: the names that are there, up to a dozen, so the test's author
    // needn't print the markup to see them.
    internal static string Present(string kind, string verb, IEnumerable<string> names)
    {
        const int Most = 12;
        var present = names.Where(name => name != "").Distinct().ToList();
        return present.Count == 0 ? ""
            : $" The {kind} are {verb}: {string.Join(", ", present.Take(Most).Select(name => $"\"{name}\""))}"
                + (present.Count > Most ? $" and {present.Count - Most} more." : ".");
    }

    // An input a user can't change, or one on a static page, which posts with its form.
    internal static void ThrowIfLocked(IElement input)
    {
        if (Locks.IsLocked(input))
        {
            throw new InvalidOperationException("This input is read-only or disabled, so a user can't change it.");
        }

        if (input.Closest("[data-static]") is not null)
        {
            throw new InvalidOperationException("On a static page this input posts with its form; render it on an "
                + "interactive page to change it.");
        }
    }

    internal static void ThrowIfStaticOrDisabled(IElement control)
    {
        if (control.TagName == "SELECT")
        {
            throw new InvalidOperationException("This field renders as a native select on a static page, which posts "
                + "with its form; render it on an interactive page to choose options.");
        }

        if (control.GetAttribute("aria-disabled") == "true")
        {
            throw new InvalidOperationException("This field is disabled, so its options can't be chosen.");
        }
    }
}

/// <summary>A <c>TbDatePicker</c>, found by its label.</summary>
public sealed class TbDatePickerDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbDatePickerDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The date the field holds, or null when it has none.</summary>
    public DateOnly? Value
    {
        get
        {
            var input = _find();
            return Dates.ReadIso(input.GetAttribute(input.HasAttribute("data-static") ? "value" : "data-value"));
        }
    }

    /// <summary>Types the date in the field's own format and commits it, as typing and leaving the field does; null
    /// clears it. A date the field can't take shows its error, as in a browser. On a static page, or when the field is
    /// read-only or disabled, this throws.</summary>
    public async Task ChooseAsync(DateOnly? date)
    {
        var input = _find();
        await EnterAsync(date?.ToString(input.GetAttribute("data-format"), Dates.CultureOf(input)) ?? "");
    }

    /// <summary>Types this text in the field as it is and commits it, as typing and leaving the field does, so a test
    /// can enter text the field can't read and see its error. On a static page, or when the field is read-only or
    /// disabled, this throws.</summary>
    public async Task EnterAsync(string text)
    {
        var input = _find();
        Dates.ThrowIfStaticOrLocked(input);
        await input.ChangeAsync(new ChangeEventArgs { Value = text });
    }
}

/// <summary>A <c>TbTimePicker</c>, found by its label.</summary>
public sealed class TbTimePickerDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbTimePickerDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The time the field holds, or null when it has none.</summary>
    public TimeOnly? Value => _find().GetAttribute("value") is { Length: > 0 } text
        ? TimeOnly.ParseExact(text, ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFF"], CultureInfo.InvariantCulture) : null;

    /// <summary>Sets the time and commits it, as filling the browser's time box does; null clears it. A time the
    /// field can't take shows its error, as in a browser. On a static page, or when the field is read-only or
    /// disabled, this throws.</summary>
    public async Task ChooseAsync(TimeOnly? time)
    {
        var input = _find();
        Typing.ThrowIfLocked(input, "a time");
        if (input.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("This field is the browser's own time box on a static page, which "
                + "posts with its form; render it on an interactive page to choose a time.");
        }

        // The browser's time box holds the time in 24-hour form, with seconds only when it has them.
        await input.ChangeAsync(new ChangeEventArgs
        {
            Value = time is { } chosen ? chosen.ToString(chosen.Second == 0 && chosen.Millisecond == 0 ? "HH:mm"
                : "HH:mm:ss.FFF", CultureInfo.InvariantCulture) : "",
        });
    }
}

/// <summary>A <c>TbDateTimePicker</c>, found by its label.</summary>
public sealed class TbDateTimePickerDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbDateTimePickerDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The date and time the field holds, or null when it has none.</summary>
    public DateTime? Value
    {
        get
        {
            var picker = _find();
            var text = picker.HasAttribute("data-static") ? picker.GetAttribute("value")
                : picker.GetAttribute("data-value");
            return string.IsNullOrEmpty(text) ? null : DateTime.ParseExact(text,
                ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"], CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Types the date in the field's own format and then the time, committing each as leaving its box does;
    /// null clears the date. On a static page, or when the field is read-only or disabled, this throws.</summary>
    public async Task ChooseAsync(DateTime? value)
    {
        var group = _find();
        await EnterAsync(value?.ToString(group.GetAttribute("data-format"), Dates.CultureOf(group)) ?? "");
        if (value is { } chosen)
        {
            await _find().QuerySelector("[data-part=time-input]")!.ChangeAsync(new ChangeEventArgs
            {
                Value = chosen.ToString(chosen.Second == 0 ? "HH:mm" : "HH:mm:ss", CultureInfo.InvariantCulture),
            });
        }
    }

    /// <summary>Types this text in the date box as it is and commits it, as leaving the box does, so a test can enter
    /// text the field can't read and see its error; the time is left unchanged. On a static page, or when the field
    /// is read-only or disabled, this throws.</summary>
    public async Task EnterAsync(string text)
    {
        var group = _find();
        var dateInput = group.QuerySelector("[data-part=date-input]")!;
        Dates.ThrowIfStaticOrLocked(group.HasAttribute("data-static") ? group : dateInput);
        await dateInput.ChangeAsync(new ChangeEventArgs { Value = text });
    }
}

/// <summary>A <c>TbDateRangePicker</c>, found by its label.</summary>
public sealed class TbDateRangePickerDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbDateRangePickerDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The range the field holds; either end is null when it has none.</summary>
    public (DateOnly? Start, DateOnly? End) Range
    {
        get
        {
            var dates = (_find().GetAttribute("data-value") ?? throw new InvalidOperationException(
                "The date range picker doesn't report its range.")).Split('/');
            return (Dates.ReadIso(dates[0]), Dates.ReadIso(dates[1]));
        }
    }

    /// <summary>Types both dates in the field's own format and commits them, as typing in each box and leaving it does;
    /// null clears that end. On a static page, or when the field is read-only or disabled, this throws.</summary>
    public async Task ChooseAsync(DateOnly? start, DateOnly? end)
    {
        var group = _find();
        var format = group.GetAttribute("data-format");
        var culture = Dates.CultureOf(group);
        await EnterAsync(start?.ToString(format, culture) ?? "", end?.ToString(format, culture) ?? "");
    }

    /// <summary>Types this text in each box as it is and commits it, as typing in the box and leaving it does, so a
    /// test can enter text the field can't read and see its error; null leaves that box unchanged. On a static page,
    /// or when the field is read-only or disabled, this throws.</summary>
    public async Task EnterAsync(string? start, string? end)
    {
        Dates.ThrowIfStaticOrLocked(_find().QuerySelector("[data-part=start-input]")!);
        foreach (var (part, text) in new[] { ("start-input", start), ("end-input", end) })
        {
            if (text is not null)
            {
                await _find().QuerySelector($"[data-part={part}]")!.ChangeAsync(new ChangeEventArgs { Value = text });
            }
        }
    }
}

internal static class Dates
{
    internal static DateOnly? ReadIso(string? text) => string.IsNullOrEmpty(text) ? null
        : DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    // The date fields read and show dates in their culture's Gregorian calendar, so typed text has to use it too.
    internal static CultureInfo CultureOf(IElement field) =>
        CalendarDates.Gregorian(CultureInfo.GetCultureInfo(field.GetAttribute("data-culture") ?? ""));

    internal static void ThrowIfStaticOrLocked(IElement input)
    {
        if (input.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("This field renders as the browser's own date box on a static page, "
                + "which posts with its form; render it on an interactive page to choose a date.");
        }

        Typing.ThrowIfLocked(input, "a date");
    }
}

// A control a user can't change: disabled, inside a disabled fieldset, or read-only by attribute or by ARIA.
internal static class Locks
{
    internal static bool IsLocked(IElement control) => control.HasAttribute("disabled")
        || control.HasAttribute("readonly") || control.Closest("fieldset[disabled]") is not null
        || control.GetAttribute("aria-readonly") == "true";
}

// A user can't type into a read-only or disabled field, so the drivers that type refuse it too.
internal static class Typing
{
    internal static void ThrowIfLocked(IElement input, string what)
    {
        if (Locks.IsLocked(input))
        {
            throw new InvalidOperationException(
                $"This field is read-only or disabled, so {what} can't be typed into it.");
        }
    }

    // The trigger and text at the end of what is typed, found as the scripts find them before the caret.
    internal static MentionQuery? MentionAt(string text, string triggers)
    {
        for (var start = text.Length - 1; start >= 0 && text.Length - start <= 50; start--)
        {
            if (char.IsWhiteSpace(text[start]))
            {
                return null;
            }

            if (triggers.Contains(text[start]) && (start == 0 || char.IsWhiteSpace(text[start - 1])
                || "([{\"'".Contains(text[start - 1])))
            {
                return new MentionQuery(text[start].ToString(), text[(start + 1)..]);
            }
        }

        return null;
    }
}

/// <summary>A <c>TbBreadcrumbs</c> trail, found by its name.</summary>
public sealed class TbBreadcrumbsDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbBreadcrumbsDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>The items' texts, from the top of the site down to the current page.</summary>
    public IReadOnlyList<string> Items => [.. Labels().Select(label => label.TextContent.Trim())];

    /// <summary>The text of the item marked as the current page, or null when none is.</summary>
    public string? Current => Labels().FirstOrDefault(label => label.GetAttribute("aria-current") == "page")
        ?.TextContent.Trim();

    /// <summary>Follows the link of the item with this text, as clicking it does: its click handler runs, and the
    /// test's <c>NavigationManager</c> goes to its address. It throws for an item without a link.</summary>
    public async Task FollowAsync(string item)
    {
        var labels = Labels().Where(label => label.TextContent.Trim() == item).ToList();
        var label = labels.Count == 1 ? labels[0] : throw new InvalidOperationException(labels.Count == 0
            ? $"The breadcrumb trail has no item that reads \"{item}\"." : $"{labels.Count} items read \"{item}\".");
        if (label.GetAttribute("data-part") != "link")
        {
            throw new InvalidOperationException($"\"{item}\" isn't a link; only an item with an Href is.");
        }

        await label.ClickAsync(new MouseEventArgs { Detail = 1 });
        _scripts.Follow(label);
    }

    // Each item holds its separator, then its link or its text.
    private IEnumerable<IElement> Labels() => _find().QuerySelectorAll(
        "[data-part=breadcrumb] > [data-part=link], [data-part=breadcrumb] > [data-part=text]");
}

/// <summary>A <c>TbStepper</c>, found by its name.</summary>
public sealed class TbStepperDriver
{
    private readonly Func<IElement> _find;

    internal TbStepperDriver(Func<IElement> find) => _find = find;

    /// <summary>The steps' titles, in order.</summary>
    public IReadOnlyList<string> Steps => [.. StepElements().Select(TitleOf)];

    /// <summary>The title of the current step, or null when no step is current.</summary>
    public string? Current => StepElements().FirstOrDefault(step =>
        step.QuerySelector(":scope > [aria-current=step]") is not null) is { } current ? TitleOf(current) : null;

    /// <summary>Goes to the step with this title, as clicking its header does. A step a user can't go to throws: one
    /// that's disabled, or, in a <c>Linear</c> stepper, one ahead that isn't finished.</summary>
    public async Task GoToAsync(string title)
    {
        var steps = StepElements().Where(step => TitleOf(step) == title).ToList();
        var step = steps.Count == 1 ? steps[0] : throw new InvalidOperationException(steps.Count == 0
            ? $"The stepper has no step titled \"{title}\"." : $"{steps.Count} steps are titled \"{title}\".");
        var button = step.QuerySelector(":scope > [data-part=step-button]") ?? throw new InvalidOperationException(
            $"The step \"{title}\" can't be chosen: it's disabled, or ahead of the current step in a linear stepper.");
        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private IEnumerable<IElement> StepElements() => _find().Children.Where(child =>
        child.GetAttribute("data-part") == "step");

    private static string TitleOf(IElement step) => step.QuerySelector("[data-part=title]")!.TextContent.Trim();
}

/// <summary>A <c>TbForm</c>, found by its name.</summary>
public sealed class TbFormDriver
{
    private readonly Func<IElement> _find;

    internal TbFormDriver(Func<IElement> find) => _find = find;

    /// <summary>The messages the form's fields show, in order.</summary>
    public IReadOnlyList<string> Errors => [.. _find().QuerySelectorAll("[data-part=error] [data-part=message]")
        .Select(message => message.TextContent.Trim())];

    /// <summary>Submits the form as its submit button does: it raises <c>OnSubmit</c> when it has one, and otherwise
    /// checks the fields and raises <c>OnValidSubmit</c> or <c>OnInvalidSubmit</c>. A disabled form ignores it, and so
    /// does a form still handling an earlier submit. Throws on a static page, where the form would post with the
    /// browser.</summary>
    public async Task SubmitAsync()
    {
        var form = _find();
        if (form.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("On a static page the form posts with the browser; render it on an "
                + "interactive page to submit it.");
        }

        await form.SubmitAsync();
    }
}

/// <summary>A <c>TbPagination</c>, found by its name.</summary>
public sealed class TbPaginationDriver
{
    private readonly Func<IElement> _find;

    internal TbPaginationDriver(Func<IElement> find) => _find = find;

    /// <summary>The page shown.</summary>
    public int Page => int.Parse(_find().GetAttribute("data-page")!, CultureInfo.InvariantCulture);

    private int PageCount => int.Parse(_find().GetAttribute("data-page-count")!, CultureInfo.InvariantCulture);

    /// <summary>Goes to this page as a user would: types it into a compact pagination's page box, or else clicks the
    /// page's button, the visible page nearest it, or Next or Previous, until it's current. A disabled pagination
    /// throws.</summary>
    public async Task GoToPageAsync(int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, PageCount);
        if (_find().QuerySelector("[data-part=page-box] input") is { } box)
        {
            if (box.HasAttribute("disabled"))
            {
                throw new InvalidOperationException("The pagination is disabled.");
            }

            await new TbNumberFieldDriver(() => _find().QuerySelector("[data-part=page-box] input")!)
                .EnterAsync(number.ToString(CultureInfo.InvariantCulture));
            if (Page != number)
            {
                throw new InvalidOperationException($"The pagination stayed on page {Page}.");
            }

            return;
        }

        while (Page is var current && current != number)
        {
            var shown = _find().QuerySelectorAll("[data-part=page]")
                .ToDictionary(page => int.Parse(page.TextContent.Trim(), CultureInfo.InvariantCulture));
            var between = shown.Keys.Where(page => number > current ? page > current && page < number
                : page < current && page > number).ToList();
            var next = shown.ContainsKey(number) ? shown[number]
                : between.Count > 0 ? shown[number > current ? between.Max() : between.Min()]
                : _find().QuerySelector(number > current ? "[data-part=next]" : "[data-part=previous]")
                ?? throw new InvalidOperationException(
                    $"The pagination can't reach page {number} from page {current}.");
            await next.ClickAsync(new MouseEventArgs { Detail = 1 });
            if (Page == current)
            {
                throw new InvalidOperationException($"The pagination stayed on page {current}; is it disabled?");
            }
        }
    }
}

/// <summary>A <c>TbAccordionItem</c>, found by its header's text.</summary>
public sealed class TbAccordionItemDriver
{
    private readonly string _header;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbAccordionItemDriver(string header, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_header, _findAll) = (header, findAll);

    /// <summary>The header's button, whose <c>aria-expanded</c> says whether the item is open.</summary>
    public IElement Trigger
    {
        get
        {
            var triggers = _findAll("button.tb-accordion__trigger")
                .Where(trigger => Choices.NameOf(trigger) == _header || TitleTexts(trigger).Contains(_header))
                .ToList();
            return triggers.Count == 1 ? triggers[0] : throw new InvalidOperationException(triggers.Count == 0
                ? $"No accordion item's header reads \"{_header}\"."
                : $"{triggers.Count} accordion items' headers read \"{_header}\".");
        }
    }

    // In a header that shows a badge, any run of title text outside the badge also names the item, so a header with a
    // count is found by its title alone.
    private static IEnumerable<string> TitleTexts(IElement trigger) =>
        trigger.QuerySelector("[data-part=badge]") is null ? [] : trigger.QuerySelectorAll(".tb-accordion__title")
            .SelectMany(title => title.Descendants<IText>())
            .Where(text => text.ParentElement?.Closest("[data-part=badge]") is null)
            .Select(text => Notices.Collapsed(text.Data));

    /// <summary>Opens the item, if it's closed, and returns its panel. Throws for a disabled item that is closed.
    /// </summary>
    public async Task<IElement> ExpandAsync()
    {
        await SetAsync(true);
        return _findAll($"[id='{Trigger.GetAttribute("aria-controls")}']").Single();
    }

    /// <summary>Closes the item, if it's open. Throws for a disabled item that is open.</summary>
    public async Task CollapseAsync() => await SetAsync(false);

    private Task SetAsync(bool expanded)
    {
        var trigger = Trigger;
        if (trigger.GetAttribute("aria-expanded") == (expanded ? "true" : "false"))
        {
            return Task.CompletedTask;
        }

        return trigger.HasAttribute("disabled")
            ? throw new InvalidOperationException($"The accordion item \"{_header}\" is disabled.")
            : trigger.ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>The toasts from <c>IToastService</c>, which the rendered <c>TbRoot</c> shows.</summary>
public sealed class TbToastsDriver
{
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbToastsDriver(Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts) =>
        (_findAll, _scripts) = (findAll, scripts);

    /// <summary>Waits for a toast whose text contains this and returns it, to read it or click its action. Like
    /// Playwright's text match, it ignores case and runs of whitespace. When several match, as when the same action
    /// ran twice, this returns the newest. When none shows within bUnit's wait timeout, this throws bUnit's
    /// <c>WaitForFailedException</c>.</summary>
    public async Task<IElement> WaitForAsync(string text)
    {
        var wanted = Collapsed(text);
        // A toast that is leaving stays in the markup while it fades; newer toasts come later in it.
        IElement? Newest() => _findAll("[data-part=toast]:not([data-leaving])")
            .LastOrDefault(toast => Collapsed(toast.TextContent).Contains(wanted, StringComparison.OrdinalIgnoreCase));
        await _scripts.WaitForAsync(() => Newest() is not null);
        return Newest()!;
    }

    /// <summary>Waits for a toast whose text contains this, as <see cref="WaitForAsync"/> does, and closes it with its
    /// dismiss button.</summary>
    public async Task DismissAsync(string text)
    {
        var toast = await WaitForAsync(text);
        await toast.QuerySelector("[data-part=close]")!.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Waits for a toast whose text contains this, as <see cref="WaitForAsync"/> does, and clicks its action
    /// button, such as <c>Undo</c>, which closes the toast and runs the action. Throws when the toast's action has
    /// another name, or it has none.</summary>
    public async Task ChooseAsync(string text, string action)
    {
        var button = (await WaitForAsync(text)).QuerySelector("[data-part=action]");
        if (button is null || Choices.NameOf(button) != action)
        {
            throw new InvalidOperationException($"The toast showing \"{text}\" has no action named \"{action}\".");
        }

        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private static string Collapsed(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

/// <summary>A <c>TbPopover</c>, found by its trigger's name.</summary>
public sealed class TbPopoverDriver
{
    private readonly string _trigger;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbPopoverDriver(string trigger, Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts) =>
        (_trigger, _findAll, _scripts) = (trigger, findAll, scripts);

    /// <summary>Opens the popover, as clicking its trigger does, and returns its panel. bUnit's markup holds the panel
    /// whether it's open or not, so this tells a bound <c>Open</c> and the popover's telemetry that it opened. A
    /// disabled trigger throws, since a user can't open it.</summary>
    public async Task<IElement> OpenAsync()
    {
        await ToggleAsync(open: true);
        return Panel;
    }

    /// <summary>The popover's panel, open or not, to read what it holds without opening it. The panel renders while
    /// closed, so this reads what the markup holds, not what a user sees.</summary>
    public IElement Panel
    {
        get
        {
            var trigger = PopoverTriggers.Find(_findAll, _trigger, "popover", refuseDisabled: false);
            return _findAll($"[id='{trigger.GetAttribute("aria-controls")}']").Single();
        }
    }

    /// <summary>Closes the popover, as Escape does, which tells a bound <c>Open</c> that it closed.</summary>
    public Task CloseAsync() => ToggleAsync(open: false);

    private async Task ToggleAsync(bool open)
    {
        var trigger = PopoverTriggers.Find(_findAll, _trigger, "popover", refuseDisabled: open);

        // A popover that attaches on first use does so when its trigger is first focused.
        if (trigger.HasAttribute("data-attach-on-use"))
        {
            await trigger.FocusAsync(new FocusEventArgs());
        }

        if (_scripts.Popover(trigger.GetAttribute("aria-controls")!) is { } events)
        {
            await _scripts.InvokeAsync(() => events.OnToggle(open));
        }
    }
}

internal static class PopoverTriggers
{
    // The trigger of the popover or popconfirm with this name, which a user can't open when it's disabled.
    internal static IElement Find(Func<string, IReadOnlyList<IElement>> findAll, string name, string kind,
        bool refuseDisabled)
    {
        var triggers = findAll("[data-part=trigger][aria-haspopup=dialog]")
            .Where(trigger => Choices.NameOf(trigger) == name).ToList();
        var trigger = triggers.Count == 1 ? triggers[0] : throw new InvalidOperationException(triggers.Count == 0
            ? $"No {kind}'s trigger is named \"{name}\"."
            : $"{triggers.Count} {kind}s' triggers are named \"{name}\".");
        if (refuseDisabled && (trigger.HasAttribute("disabled") || trigger.GetAttribute("aria-disabled") == "true"))
        {
            throw new InvalidOperationException($"The {kind} \"{name}\" is disabled.");
        }

        return trigger;
    }
}

/// <summary>A <c>TbAlert</c>, found by its text.</summary>
public sealed class TbAlertDriver
{
    private readonly Func<IElement> _find;

    internal TbAlertDriver(Func<IElement> find) => _find = find;

    /// <summary>The alert's message, without its title, with runs of whitespace collapsed.</summary>
    public string Text => Notices.Message(_find());

    /// <summary>Presses the close button, which raises <c>OnClose</c>. Only an alert with an <c>OnClose</c> has the
    /// button, so this throws for one without, and on a static page, where the button does nothing.</summary>
    public async Task CloseAsync() => await Notices.CloseAsync(_find());
}

/// <summary>A <c>TbBanner</c>, found by its name.</summary>
public sealed class TbBannerDriver
{
    private readonly Func<IElement> _find;

    internal TbBannerDriver(Func<IElement> find) => _find = find;

    /// <summary>The banner's message, without its title, with runs of whitespace collapsed.</summary>
    public string Text => Notices.Message(_find());

    /// <summary>Presses the close button, which raises <c>OnClose</c>. Only a banner with an <c>OnClose</c> has the
    /// button, so this throws for one without, and on a static page, where the button does nothing.</summary>
    public async Task CloseAsync() => await Notices.CloseAsync(_find());
}

internal static class Notices
{
    // A banner's title sits inside its message part, so the text is read from a copy with the title removed.
    internal static string Message(IElement notice)
    {
        var message = (IElement)notice.QuerySelector("[data-part=message]")!.Clone();
        message.QuerySelector("[data-part=title]")?.Remove();
        return Collapsed(message.TextContent);
    }

    internal static async Task CloseAsync(IElement notice)
    {
        if (notice.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("On a static page the close button does nothing; render the page "
                + "interactively to close it.");
        }

        var close = notice.QuerySelector("[data-part=close]")
            ?? throw new InvalidOperationException("There's no close button; it shows only when OnClose is set.");
        await close.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    internal static string Collapsed(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

/// <summary>A <c>TbVirtualList</c>, found by its <c>AriaLabel</c>.</summary>
public sealed class TbVirtualListDriver
{
    private readonly Func<IElement> _find;

    internal TbVirtualListDriver(Func<IElement> find) => _find = find;

    /// <summary>How many items the list has, drawn or not; 0 while its <c>ItemsProvider</c> first loads.</summary>
    public int Count => int.Parse(_find().GetAttribute("data-count")!, CultureInfo.InvariantCulture);

    /// <summary>The item at this index, from 0. bUnit runs no script, so the list draws only the items its first
    /// view holds; an item it hasn't drawn throws; test that one with the Playwright driver, which scrolls to it.
    /// </summary>
    public IElement Item(int index)
    {
        var list = _find();
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
        return list.QuerySelector($":scope > [data-part=item][data-index='{index}']")
            ?? throw new InvalidOperationException($"Item {index} isn't drawn. bUnit runs no script, so the list "
                + "draws only the items its first view holds; test a far item with the Playwright driver.");
    }
}

/// <summary>A <c>TbChat</c>, found by its <c>AriaLabel</c>.</summary>
public sealed class TbChatDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbChatDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>Each message, oldest first, as its author's name and its text, such as <c>"Robin: Meet at nine"</c>.
    /// </summary>
    public IReadOnlyList<string> Messages => [.. _find()
        .QuerySelectorAll(":scope > [data-part=messages] > [data-part=message]")
        .Select(message => $"{message.QuerySelector("[data-part=author]")!.TextContent.Trim()}: "
            + message.QuerySelector("[data-part=body]")!.TextContent.Trim())];

    /// <summary>Who the chat says is typing, such as <c>"Robin is typing…"</c>, or null when no one is.</summary>
    public string? Typing => _find().QuerySelector(":scope > [data-part=typing]")?.TextContent.Trim();

    /// <summary>Types the text in the box and sends it, as pressing Enter does, which raises <c>OnSend</c>. A chat
    /// without its composer, or with <c>Disabled</c> set, throws.</summary>
    public async Task SendAsync(string text)
    {
        var composer = Composer();
        await composer.QuerySelector("[data-part=input]")!.InputAsync(new ChangeEventArgs { Value = text });
        await composer.SubmitAsync();
    }

    /// <summary>Chooses the files at these paths with the attach button, to go with the next message. A chat without
    /// its composer or <c>AllowAttachments</c>, with <c>Disabled</c> set, or still waiting on <c>OnSend</c>, throws.
    /// </summary>
    public async Task AttachAsync(params string[] paths)
    {
        ArgumentOutOfRangeException.ThrowIfZero(paths.Length, nameof(paths));
        Files().UploadFiles(TbFileUploadDriver.FromPaths(paths));
    }

    /// <summary>Chooses files made in the test, such as <c>InputFileContent.CreateFromText("Day one", "route.txt")</c>,
    /// to go with the next message. Throws as the paths overload does.</summary>
    public async Task AttachAsync(params InputFileContent[] files)
    {
        ArgumentOutOfRangeException.ThrowIfZero(files.Length, nameof(files));
        Files().UploadFiles(files);
    }

    private IElement Composer()
    {
        var composer = _find().QuerySelector(":scope > [data-part=composer]")
            ?? throw new InvalidOperationException("This chat has no composer; turn ShowComposer on.");
        return composer.QuerySelector("[data-part=input]")!.HasAttribute("disabled")
            ? throw new InvalidOperationException("This chat is disabled.")
            : composer;
    }

    private IRenderedComponent<InputFile> Files()
    {
        var input = Composer().QuerySelector("[data-part=attach] input[type=file]")
            ?? throw new InvalidOperationException("This chat doesn't take files; turn AllowAttachments on.");
        return input.HasAttribute("disabled")
            ? throw new InvalidOperationException("This chat is still sending; attach once OnSend has run.")
            : _scripts.InputFile(input.Id!);
    }
}

/// <summary>A <c>TbAiPrompt</c>, found by its <c>Label</c>. Responses are newest first, so index 0 is the latest.
/// </summary>
public sealed class TbAiPromptDriver
{
    private readonly Func<IElement> _find;

    internal TbAiPromptDriver(Func<IElement> find) => _find = find;

    /// <summary>Each response's text, newest first.</summary>
    public IReadOnlyList<string> Responses => [.. Outputs().Select(output =>
        output.QuerySelector("[data-part=text]")!.TextContent.Trim())];

    /// <summary>Each response's status, newest first.</summary>
    public IReadOnlyList<AiPromptStatus> Statuses => [.. Outputs().Select(output =>
        Enum.Parse<AiPromptStatus>(output.GetAttribute("data-status")!, ignoreCase: true))];

    /// <summary>Types the prompt in the box and generates, as pressing Enter does. A disabled prompt, or one still
    /// generating, throws.</summary>
    public async Task PromptAsync(string prompt)
    {
        var composer = Composer();
        await composer.QuerySelector("[data-part=input]")!.InputAsync(new ChangeEventArgs { Value = prompt });
        await composer.SubmitAsync();
    }

    /// <summary>Clicks the suggestion with this text, which puts it in the box. A prompt without it throws.</summary>
    public async Task ChooseSuggestionAsync(string suggestion)
    {
        var button = Composer().QuerySelectorAll("[data-part=suggestion]")
            .FirstOrDefault(button => button.TextContent.Trim() == suggestion)
            ?? throw new InvalidOperationException($"This AI prompt has no suggestion \"{suggestion}\".");
        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Stops the response that's generating, as Escape does. A prompt with nothing generating throws.
    /// </summary>
    public async Task StopAsync()
    {
        var prompt = _find();
        if (!prompt.HasAttribute("data-generating"))
        {
            throw new InvalidOperationException("Nothing is generating in this AI prompt.");
        }

        await prompt.QuerySelector(":scope > [data-part=composer] [data-part=generate]")!
            .ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Copies a response's text; the app's <c>FakeBrowserService</c> records it. A response with no text
    /// yet throws.</summary>
    public Task CopyAsync(int index = 0) => PressAsync(index, "copy", "Copy");

    /// <summary>Generates a response again for its prompt. A prompt that's disabled or still generating throws.
    /// </summary>
    public Task RetryAsync(int index = 0) => PressAsync(index, "retry", "Retry");

    /// <summary>Inserts a response, which raises <c>OnInsert</c>. A prompt without <c>OnInsert</c> or that's disabled,
    /// or a response with no text yet, throws.</summary>
    public Task InsertAsync(int index = 0) => PressAsync(index, "insert", "Insert");

    private List<IElement> Outputs() =>
        [.. _find().QuerySelectorAll(":scope > [data-part=outputs] > [data-part=output]")];

    private IElement Composer()
    {
        var prompt = _find();
        var composer = prompt.QuerySelector(":scope > [data-part=composer]")!;
        if (composer.QuerySelector("[data-part=input]")!.HasAttribute("disabled"))
        {
            throw new InvalidOperationException("This AI prompt is disabled.");
        }

        return prompt.HasAttribute("data-generating")
            ? throw new InvalidOperationException("This AI prompt is still generating; stop it or let it finish.")
            : composer;
    }

    private async Task PressAsync(int index, string part, string name)
    {
        var outputs = Outputs();
        if (index < 0 || index >= outputs.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index,
                $"There's no response at index {index}; this AI prompt has {outputs.Count}.");
        }

        var button = outputs[index].QuerySelector($"[data-part={part}]")
            ?? throw new InvalidOperationException("This AI prompt has no Insert; set OnInsert.");
        if (button.HasAttribute("disabled"))
        {
            throw new InvalidOperationException(
                $"{name} is off for this response: it has no text yet, or the prompt is disabled or generating.");
        }

        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>A <c>TbCarousel</c>, found by its <c>AriaLabel</c>.</summary>
public sealed class TbCarouselDriver
{
    private readonly Func<IElement> _find;

    internal TbCarouselDriver(Func<IElement> find) => _find = find;

    /// <summary>How many slides there are.</summary>
    public int Count => Slides().Count;

    /// <summary>The index of the slide shown, from 0. During a move between slides, it's the slide arriving.
    /// </summary>
    public int ActiveIndex => Slides().FindIndex(slide => !slide.HasAttribute("hidden") && !slide.HasAttribute("inert"));

    /// <summary>Shows the next slide, as pressing Next does. On the last slide of a carousel that doesn't loop, Next
    /// is disabled, so this throws, as it does on a static page.</summary>
    public Task NextAsync() => PressAsync("next");

    /// <summary>Shows the previous slide, as pressing Previous does. On the first slide of a carousel that doesn't
    /// loop, Previous is disabled, so this throws, as it does on a static page.</summary>
    public Task PreviousAsync() => PressAsync("previous");

    /// <summary>Shows the slide at this index, from 0, as pressing its button in the picker does. A carousel without
    /// a picker throws, and so does one on a static page.</summary>
    public async Task GoToAsync(int index)
    {
        var picks = Unlocked().QuerySelectorAll(":scope > [data-part=picker] > [data-part=pick]");
        if (picks.Length == 0)
        {
            throw new InvalidOperationException("This carousel has no picker; use NextAsync and PreviousAsync.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, picks.Length);
        await picks[index].ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private async Task PressAsync(string part)
    {
        var button = Unlocked().QuerySelector($":scope > [data-part=controls] > [data-part={part}]")!;
        if (button.GetAttribute("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"There's no {part} slide; the carousel doesn't loop.");
        }

        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private IElement Unlocked() => _find() is var carousel && carousel.HasAttribute("data-static")
        ? throw new InvalidOperationException("On a static page the carousel's buttons do nothing; render the page "
            + "interactively to move it.")
        : carousel;

    private List<IElement> Slides() =>
        [.. _find().QuerySelectorAll(":scope > [data-part=slides] > [data-part=slide]")];
}

/// <summary>A <c>TbChip</c>, found by its text.</summary>
public sealed class TbChipDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbChipDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>Whether a chip that toggles is selected.</summary>
    public bool Selected => _find().QuerySelector("[data-part=action]")!.GetAttribute("aria-pressed") == "true";

    /// <summary>Clicks the chip, as a user does: a chip that toggles is selected or cleared, one with an
    /// <c>OnClick</c> runs it, and one with an <c>Href</c> follows its link, taking the test's
    /// <c>NavigationManager</c> to its address. It throws for a chip that does nothing when clicked, a disabled chip,
    /// or one on a static page.</summary>
    public async Task ClickAsync()
    {
        var action = ChipParts.Unlocked(_find()).QuerySelector("[data-part=action]")!;
        if (action.LocalName is not ("button" or "a"))
        {
            throw new InvalidOperationException("This chip does nothing when clicked; give it an OnClick, an Href or a "
                + "SelectedChanged.");
        }

        // A link chip with no OnClick renders no click handler, so following its link is all a click does.
        if (action.HasAttribute("blazor:onclick"))
        {
            await action.ClickAsync(new MouseEventArgs { Detail = 1 });
        }

        _scripts.Follow(action);
    }

    /// <summary>Presses the chip's remove button, which raises <c>OnRemove</c>. A chip with no <c>OnRemove</c> has no
    /// remove button, so this throws, as it does for a disabled chip or one on a static page.</summary>
    public async Task RemoveAsync()
    {
        var remove = ChipParts.Unlocked(_find()).QuerySelector("[data-part=remove]") ?? throw new
            InvalidOperationException("This chip has no remove button; it shows only when OnRemove is set.");
        await remove.ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>A <c>TbChipGroup</c>, found by its <c>AriaLabel</c>.</summary>
public sealed class TbChipGroupDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbChipGroupDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>The chips' texts, in order.</summary>
    public IReadOnlyList<string> Chips => [.. ChipParts.Only([.. _find().QuerySelectorAll(".tb-chip")])
        .Select(ChipParts.TextOf)];

    /// <summary>The chip in the group whose text is this.</summary>
    public TbChipDriver Chip(string text) =>
        new(() => ChipParts.Find([.. _find().QuerySelectorAll(".tb-chip")], text), _scripts);
}

internal static class ChipParts
{
    internal static string TextOf(IElement chip) =>
        chip.QuerySelector("[data-part=label]")!.TextContent.Trim();

    // A tag input's tags and a multi-select's chosen options share the chip's class, but not its parts.
    internal static IEnumerable<IElement> Only(IEnumerable<IElement> chips) =>
        chips.Where(chip => chip.QuerySelector(":scope > [data-part=action]") is not null);

    internal static IElement Find(IReadOnlyList<IElement> chips, string text)
    {
        var found = Only(chips).Where(chip => TextOf(chip) == text).ToList();
        return found.Count == 1 ? found[0] : throw new InvalidOperationException(found.Count == 0
            ? $"No chip shows \"{text}\"." : $"{found.Count} chips show \"{text}\".");
    }

    internal static IElement Unlocked(IElement chip)
    {
        if (chip.ClassList.Contains("tb-chip--disabled"))
        {
            throw new InvalidOperationException("This chip is disabled, so a user can't click it.");
        }

        return chip.HasAttribute("data-static") ? throw new InvalidOperationException("On a static page a chip's "
            + "buttons do nothing; render the page interactively to use them.") : chip;
    }
}

/// <summary>A <c>TbButton</c> or <c>TbIconButton</c>, found by its accessible name.</summary>
public sealed class TbButtonDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbButtonDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>Whether the button is busy, as <c>Loading</c> or a click still running makes it; a busy button ignores
    /// clicks.</summary>
    public bool Busy => _find().GetAttribute("aria-busy") == "true";

    /// <summary>Whether a toggle button is pressed.</summary>
    public bool Pressed => _find().GetAttribute("aria-pressed") == "true";

    /// <summary>Whether a toggle button shows its mixed state, on for only part of what it acts on.</summary>
    public bool Mixed => _find().GetAttribute("aria-pressed") == "mixed";

    /// <summary>Clicks the button, as a user does, which raises its <c>OnClick</c>; a link button that opens in the
    /// same tab also takes the test's <c>NavigationManager</c> to its address. Throws for a busy or disabled button, and
    /// on a static page for any button but a link, since there it does nothing or the browser posts its form.</summary>
    public async Task ClickAsync()
    {
        var button = _find();
        if (button.GetAttribute("aria-busy") == "true")
        {
            throw new InvalidOperationException("The button is busy, so it ignores clicks until its Loading, its "
                + "form's submit or its running click ends.");
        }

        if (Choices.IsDisabled(button))
        {
            throw new InvalidOperationException("The button is disabled, so a user can't click it.");
        }

        if (button.LocalName != "a" && button.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("On a static page the button does nothing, or the browser posts its "
                + "form; render the page interactively to use it.");
        }

        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
        _scripts.Follow(button);
    }
}

/// <summary>A <c>TbLink</c>, found by its accessible name.</summary>
public sealed class TbLinkDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbLinkDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>Clicks the link, as a user does, which raises its <c>OnClick</c> and waits until it has run; a link
    /// with an address that opens in the same tab also takes the test's <c>NavigationManager</c> there. Throws for a
    /// disabled link, and on a static page for a link without an address, since there it does nothing.</summary>
    public async Task ClickAsync()
    {
        var link = _find();
        if (Choices.IsDisabled(link))
        {
            throw new InvalidOperationException("The link is disabled, so a user can't click it.");
        }

        if (link.LocalName != "a" && link.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("On a static page a link without an address does nothing; render the "
                + "page interactively to use it.");
        }

        await link.ClickAsync(new MouseEventArgs { Detail = 1 });
        _scripts.Follow(link);
    }
}

/// <summary>A <c>TbNavMenu</c>, found by its navigation landmark's name.</summary>
public sealed class TbNavMenuDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbNavMenuDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>The text of the link marked as the current page, or null when none is. A link stays current on the
    /// pages under its address, so when several are current, it's the one with the longest address.</summary>
    public string? Current => _find().QuerySelectorAll("[data-part=link][aria-current=page]")
        .MaxBy(link => link.GetAttribute("href")!.Length)?.QuerySelector(":scope > [data-part=label]")!.TextContent
        .Trim();

    /// <summary>Follows the link with this text, through the groups named before it, as in
    /// <c>FollowAsync("Settings", "Profile")</c>. Each closed group opens as clicking it does, then the link's click
    /// runs and the test's <c>NavigationManager</c> goes to its address. Throws for a link in a closed group the path
    /// doesn't name, or for a closed group on a static page, which can't open.</summary>
    public async Task FollowAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        await OpenAsync(path[..^1]);
        var link = Only(Within(path[..^1]), "[data-part=link]", path[^1], "links");
        if (link.Closest("[data-part=group-items][hidden]") is not null)
        {
            throw InClosedGroup(path[^1]);
        }

        await link.ClickAsync(new MouseEventArgs { Detail = 1 });
        _scripts.Follow(link);
    }

    /// <summary>Whether the group at the end of this path is open, as in <c>Expanded("Settings", "Advanced")</c>.
    /// The groups before it are left as they are.</summary>
    public bool Expanded(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        return Only(Within(path[..^1]), "[data-part=group-toggle]", path[^1], "groups")
            .GetAttribute("aria-expanded") == "true";
    }

    /// <summary>Opens each closed group on this path, as in <c>ExpandAsync("Settings", "Advanced")</c>. Each opens as
    /// clicking it does, and the app's <c>ExpandedChanged</c> runs. Throws for a group in a closed group the path
    /// doesn't name, or for a closed group on a static page.</summary>
    public async Task ExpandAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        await OpenAsync(path);
    }

    /// <summary>Closes the group at the end of this path, if it's open, as clicking it does, and the app's
    /// <c>ExpandedChanged</c> runs. The groups before it open first, as <c>ExpandAsync</c> opens them. Throws for a
    /// group in a closed group the path doesn't name, or on a static page for a group it would have to open or close.
    /// </summary>
    public async Task CollapseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        await OpenAsync(path[..^1]);
        var toggle = VisibleToggle(path);
        if (toggle.GetAttribute("aria-expanded") == "true")
        {
            await ToggleAsync(toggle, path[^1], false);
        }
    }

    private static InvalidOperationException InClosedGroup(string item) => new($"\"{item}\" is in a closed group; "
        + "name the groups that contain it earlier in the path.");

    private async Task OpenAsync(string[] groups)
    {
        for (var depth = 1; depth <= groups.Length; depth++)
        {
            var toggle = VisibleToggle(groups[..depth]);
            if (toggle.GetAttribute("aria-expanded") != "true")
            {
                await ToggleAsync(toggle, groups[depth - 1], true);
            }
        }
    }

    // The button of the group at the end of the path, which mustn't be in a closed group the path doesn't name.
    private IElement VisibleToggle(string[] path)
    {
        var toggle = Only(Within(path[..^1]), "[data-part=group-toggle]", path[^1], "groups");
        return toggle.Closest("[data-part=group-items][hidden]") is null ? toggle : throw InClosedGroup(path[^1]);
    }

    private async Task ToggleAsync(IElement toggle, string group, bool expanded)
    {
        if (_find().HasAttribute("data-static"))
        {
            throw new InvalidOperationException($"On a static page the group \"{group}\" doesn't "
                + $"{(expanded ? "open" : "close")}; render the page interactively to use it.");
        }

        await toggle.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    // The menu, or the links of the group the path ends at. Found again for each step, since opening a group renders
    // new markup.
    private IElement Within(string[] groups)
    {
        var within = _find();
        foreach (var group in groups)
        {
            within = Only(within, "[data-part=group-toggle]", group, "groups").ParentElement!
                .QuerySelector(":scope > [data-part=group-items]")!;
        }

        return within;
    }

    // A match at this level wins over same-labeled ones in nested groups, so a label repeated deeper doesn't hide it.
    private static IElement Only(IElement within, string selector, string text, string kind)
    {
        var found = within.QuerySelectorAll(selector)
            .Where(part => part.QuerySelector("[data-part=label]")!.TextContent.Trim() == text).ToList();
        var own = found.Where(part => part.Closest("ul") == within).ToList();
        found = own.Count > 0 ? own : found;
        return found.Count == 1 ? found[0] : throw new InvalidOperationException(found.Count == 0
            ? $"The nav menu has no {kind[..^1]} that reads \"{text}\"." : $"{found.Count} {kind} read \"{text}\".");
    }
}

/// <summary>A <c>TbInsertDivider</c>, found by its <c>Label</c>.</summary>
public sealed class TbInsertDividerDriver
{
    private readonly Func<IElement> _find;

    internal TbInsertDividerDriver(Func<IElement> find) => _find = find;

    /// <summary>Presses the insert button, which raises <c>OnInsert</c>. Throws for a disabled divider, or on a static
    /// page, where the button does nothing.</summary>
    public async Task InsertAsync()
    {
        var divider = _find();
        if (divider.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("On a static page the insert button does nothing; render the page "
                + "interactively to use it.");
        }

        var insert = divider.QuerySelector("[data-part=insert]")!;
        if (Choices.IsDisabled(insert))
        {
            throw new InvalidOperationException("This insert divider is disabled.");
        }

        await insert.ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>A <c>TbText</c> with <c>ShowFullText</c> or <c>MaxLength</c>, found by its whole text.</summary>
public sealed class TbTextDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;
    private TextCut? _cut;

    internal TbTextDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>Whether the text is cut off. <c>MaxLength</c> cuts it as it renders. Only a browser can measure
    /// whether <c>MaxLines</c> does, so in bUnit it is cut once <see cref="SetCutAsync"/> says so.</summary>
    public bool IsCut => _find().HasAttribute("data-cut");

    /// <summary>The whole text, which screen readers read and which the tooltip shows when the text is cut off.
    /// </summary>
    public string FullText => FullTextOf(_find())!;

    /// <summary>The text as it shows: cut to <c>MaxLength</c>, at the end or in the middle, or whole. Only a browser
    /// fits a middle cut to its box, so in bUnit this reads the <c>MaxLength</c> cut or the whole text.</summary>
    public string ShownText => _find().QuerySelector(":scope > [data-part=shortened]")?.TextContent ?? FullText;

    /// <summary>Tells text with <c>MaxLines</c> and <c>ShowFullText</c> whether its box cuts it off, as its script does
    /// in a browser whenever the box or the text changes.</summary>
    public Task SetCutAsync(bool cut) => _scripts.InvokeAsync(() => (_cut ??= _scripts.Text(_find())).OnCut(cut, false));

    // Read as a browser shows it, with the markup's line breaks and indents as single spaces.
    internal static string? FullTextOf(IElement text) =>
        text.QuerySelector(":scope > [data-part=full-text]") is { } full
            ? string.Join(' ', full.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            : null;
}

/// <summary>A <c>TbSpeechToText</c>, found by its button's <c>Label</c>.</summary>
public sealed class TbSpeechToTextDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbSpeechToTextDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>Whether the button shows, which it does where the browser recognizes speech.</summary>
    public bool IsAvailable => _find().Closest(".tb-speech-to-text")?.HasAttribute("hidden") == false;

    /// <summary>Whether it is listening.</summary>
    public bool IsListening => _find().GetAttribute("aria-pressed") == "true";

    /// <summary>Presses the button to start listening. Throws while it's listening already, or hidden.</summary>
    public Task StartAsync() => PressAsync(listening: false);

    /// <summary>Presses the button to stop listening. Throws while it isn't listening.</summary>
    public Task StopAsync() => PressAsync(listening: true);

    /// <summary>Hands this phrase to the app as the browser would on hearing it, which raises <c>OnText</c>. Without
    /// <c>Continuous</c> it then stops listening, as the browser does after one phrase. Throws while it isn't
    /// listening; start it with <see cref="StartAsync"/> first.</summary>
    public async Task SayAsync(string text)
    {
        if (!IsListening)
        {
            throw new InvalidOperationException("The dictation button isn't listening; call StartAsync first.");
        }

        var events = _scripts.SpeechToText(_find());
        await _scripts.InvokeAsync(async () =>
        {
            var ends = events.SessionAPhraseEnds;
            await events.OnText(text);
            if (ends is { } session)
            {
                await events.OnStopped(session);
            }
        });
    }

    private async Task PressAsync(bool listening)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("The dictation button is hidden; it stays hidden until its script "
                + "finds the browser's speech recognition.");
        }

        if (IsListening != listening)
        {
            throw new InvalidOperationException(listening ? "The dictation button isn't listening."
                : "The dictation button is listening already.");
        }

        await _find().ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>A <c>TbInPlaceEditor</c>, found by its <c>Label</c>.</summary>
public sealed class TbInPlaceEditorDriver
{
    private readonly Func<IElement> _find;

    internal TbInPlaceEditorDriver(Func<IElement> find) => _find = find;

    /// <summary>Whether the editor shows, rather than the value.</summary>
    public bool IsEditing => _find().HasAttribute("data-editing");

    /// <summary>The value's text while it shows, or the placeholder while it's empty.</summary>
    public string Text => _find().QuerySelector("[data-part=value]")?.TextContent.Trim()
        ?? throw new InvalidOperationException("The in-place editor is editing; save or cancel it to read its value.");

    /// <summary>Presses the value to start editing it. Throws while it's editing already, or disabled.</summary>
    public async Task EditAsync()
    {
        var display = _find().QuerySelector("[data-part=display]")
            ?? throw new InvalidOperationException("The in-place editor is editing already.");
        if (display.HasAttribute("disabled"))
        {
            throw new InvalidOperationException("This in-place editor is disabled.");
        }

        await display.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Presses the save button, which sets the value to the draft. Throws while the value shows.</summary>
    public Task SaveAsync() => PressAsync("save");

    /// <summary>Presses the cancel button, which throws the draft away. Throws while the value shows.</summary>
    public Task CancelAsync() => PressAsync("cancel");

    private async Task PressAsync(string part)
    {
        var button = _find().QuerySelector($"[data-part={part}]")
            ?? throw new InvalidOperationException("The in-place editor isn't editing; call EditAsync first.");
        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>A <c>TbDescriptionList</c>: terms and their values.</summary>
public sealed class TbDescriptionListDriver
{
    private readonly Func<IElement> _find;

    internal TbDescriptionListDriver(Func<IElement> find) => _find = find;

    /// <summary>The terms, in order.</summary>
    public IReadOnlyList<string> Terms =>
        [.. Items().Select(item =>
            Notices.Collapsed(item.QuerySelector(":scope > [data-part=term]")!.TextContent))];

    /// <summary>The value of the term with this text, as it shows, or an empty string for a blank value, which shows a
    /// dash. Throws when no term has the text.</summary>
    public string ValueOf(string term)
    {
        var item = Items().FirstOrDefault(each =>
                Notices.Collapsed(each.QuerySelector(":scope > [data-part=term]")!.TextContent) == term)
            ?? throw new InvalidOperationException($"No term reads \"{term}\"; the terms are "
                + string.Join(", ", Terms.Select(each => $"\"{each}\"")) + ".");
        // A blank value's dash and the words screen readers hear for it, and a tooltip's bubble, aren't shown text.
        var value = (IElement)item.QuerySelector(":scope > [data-part=value]")!.Clone();
        foreach (var hidden in value.QuerySelectorAll("[aria-hidden=true], .tb-visually-hidden, [role=tooltip]"))
        {
            hidden.Remove();
        }

        return Notices.Collapsed(value.TextContent);
    }

    // The list's own pairs, not those of another list inside a value.
    private IEnumerable<IElement> Items() =>
        _find().QuerySelectorAll(":scope > [data-part=list] > [data-part=item]");
}

/// <summary>A <c>TbAsyncContent</c>, found by its <c>Label</c>.</summary>
public sealed class TbAsyncContentDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbAsyncContentDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>What it shows: loading, failed, empty, no matches or the content. A reload keeps the state of what it
    /// shows; <see cref="Busy"/> says it's loading.</summary>
    public AsyncContentState State =>
        Enum.Parse<AsyncContentState>(_find().GetAttribute("data-state")!.Replace("-", ""), ignoreCase: true);

    /// <summary>Whether a load is running, a first load or a reload.</summary>
    public bool Busy => State == AsyncContentState.Loading || _find().GetAttribute("aria-busy") == "true";

    /// <summary>Waits until no load is running and the first one has finished, loaded or failed.</summary>
    public async Task WaitUntilLoadedAsync()
    {
        _find();
        await _scripts.WaitForAsync(() => !Busy && State != AsyncContentState.Loading);
    }

    /// <summary>Presses the Retry button a failed load shows, and returns once the load it starts has finished. Throws
    /// when no Retry button shows: the load hasn't failed, or its <c>FailedContent</c> has buttons of its own.
    /// </summary>
    public async Task RetryAsync()
    {
        var retry = _find().QuerySelector(":scope > [data-part=failed] [data-part=retry]")
            ?? throw new InvalidOperationException(State == AsyncContentState.Failed
                ? "The failure shows no Retry button; its FailedContent has buttons of its own."
                : "No load has failed, so there's no Retry button.");
        await retry.ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>A <c>TbSmartPaste</c>, found by its button's <c>Label</c>.</summary>
public sealed class TbSmartPasteDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbSmartPasteDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_find, _findAll) = (find, findAll);

    /// <summary>What it last showed beside the button, such as "Filled 3 fields", or an empty string before the first
    /// paste.</summary>
    public string Status => _find().QuerySelector("[data-part=status]")!.TextContent.Trim();

    /// <summary>Presses the button, enters this text in the box that opens, and fills the form from it, which calls
    /// <c>Extract</c>. bUnit has no clipboard to read, so the box opens as it does where the browser won't share the
    /// clipboard. A disabled button throws.</summary>
    public async Task PasteAsync(string text)
    {
        var button = _find().QuerySelector("[data-part=button]")!;
        if (button.HasAttribute("disabled"))
        {
            throw new InvalidOperationException("The smart paste button is disabled.");
        }

        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
        await Box().QuerySelector("[data-part=text]")!.ChangeAsync(new ChangeEventArgs { Value = text });
        await Box().QuerySelector("[data-part=fill]")!.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    // A modal box opens one at a time, so the open one is this button's.
    private IElement Box() => _findAll("dialog.tb-smart-paste__box:not([data-leaving])").SingleOrDefault()
        ?? throw new InvalidOperationException("The paste box didn't open. It opens only when the clipboard can't be "
            + "read, so a test that sets up the script's readClipboard to return text fills the form without it.");
}

/// <summary>The page's <c>TbBackToTop</c>.</summary>
public sealed class TbBackToTopDriver
{
    private readonly Func<IElement> _find;

    internal TbBackToTopDriver(Func<IElement> find) => _find = find;

    /// <summary>Presses the button. In bUnit it is there whether or not the page has scrolled, since only a browser
    /// scrolls.</summary>
    public async Task ClickAsync() =>
        await _find().QuerySelector("[data-part=button]")!.ClickAsync(new MouseEventArgs { Detail = 1 });
}

/// <summary>A <c>TbToolbar</c>, found by its <c>AriaLabel</c>.</summary>
public sealed class TbToolbarDriver
{
    private readonly Func<IElement> _find;

    internal TbToolbarDriver(Func<IElement> find) => _find = find;

    /// <summary>Clicks the toolbar's button with this name, as a user does. bUnit lays nothing out, so no button moves
    /// into the overflow menu. Throws for a disabled button, or on a static page, where the buttons do nothing.
    /// </summary>
    public async Task ClickAsync(string button)
    {
        var toolbar = _find();
        if (toolbar.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("On a static page the toolbar's buttons do nothing; render the page "
                + "interactively to use them.");
        }

        // The overflow menu's button, hidden until a button moves into the menu, isn't one of the toolbar's own, and
        // a menu's items are <button> elements with a menu item role.
        var buttons = toolbar.QuerySelectorAll("button:not([role]), button[role=button]")
            .Where(found => found.Closest("[data-part=overflow]") is null && Choices.NameOf(found) == button).ToList();
        var only = buttons.Count == 1 ? buttons[0] : throw new InvalidOperationException(buttons.Count == 0
            ? $"The toolbar has no button named \"{button}\"."
            : $"The toolbar has {buttons.Count} buttons named \"{button}\".");
        if (Choices.IsDisabled(only))
        {
            throw new InvalidOperationException($"The toolbar button \"{button}\" is disabled.");
        }

        await only.ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>A <c>TbRibbon</c>, found by its <c>AriaLabel</c>.</summary>
public sealed class TbRibbonDriver
{
    private readonly Func<IElement> _find;
    private readonly TbTabsDriver _tabs;

    internal TbRibbonDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts)
    {
        _find = find;
        _tabs = new(() => find().QuerySelector(":scope > .tb-tabs > [role=tablist]")!, findAll, scripts);
    }

    /// <summary>The tabs' titles, in order.</summary>
    public IReadOnlyList<string> Tabs => _tabs.Tabs;

    /// <summary>The selected tab's title.</summary>
    public string? Selected => _tabs.Selected;

    /// <summary>Selects the tab with this title, as clicking it does.</summary>
    public Task SelectAsync(string tab) => _tabs.SelectAsync(tab);

    /// <summary>The names of the selected tab's groups, in order.</summary>
    public IReadOnlyList<string> Groups => Own(Commands?.QuerySelectorAll("[data-part=group-label]"))
        .Select(label => label.TextContent.Trim()).ToList();

    /// <summary>Whether the ribbon is collapsed to its tabs.</summary>
    public bool IsCollapsed => _find().HasAttribute("data-collapsed");

    /// <summary>Collapses the ribbon to its tabs with its collapse button, unless it already is. Throws for a ribbon
    /// without <c>Collapsible</c>, which has no such button, or on a static page.</summary>
    public Task CollapseAsync() => SetCollapsedAsync(true);

    /// <summary>Shows a collapsed ribbon's commands again with its collapse button, unless they already show. Throws
    /// for a ribbon without <c>Collapsible</c>, which has no such button, or on a static page.</summary>
    public Task ExpandAsync() => SetCollapsedAsync(false);

    private async Task SetCollapsedAsync(bool collapsed)
    {
        if (_find().HasAttribute("data-static"))
        {
            throw new InvalidOperationException("On a static page the ribbon can't collapse; render the page "
                + "interactively to collapse it.");
        }

        var button = Own(_find().QuerySelectorAll("[data-part=collapse]")).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "The ribbon has no collapse button; set Collapsible to give it one.");
        if (IsCollapsed != collapsed)
        {
            await button.ClickAsync(new MouseEventArgs { Detail = 1 });
        }
    }

    /// <summary>Clicks the button with this name in the selected tab, as a user does. Throws for a disabled button,
    /// a name the selected tab doesn't have, or a static page, where the buttons do nothing. bUnit runs no script, so
    /// a collapsed ribbon's button is clicked directly, without showing its commands first.</summary>
    public async Task ClickAsync(string button)
    {
        // Leaves out a menu's items, which are <button> elements with a menu item role, and the buttons that groups
        // collapse into, which only a browser shows.
        var all = Commands?.QuerySelectorAll("button:not([role], [data-part=group-button]), button[role=button]");
        var buttons = Own(all).Where(found => Choices.NameOf(found) == button).ToList();
        var only = buttons.Count == 1 ? buttons[0] : throw new InvalidOperationException(buttons.Count == 0
            ? $"The ribbon's selected tab has no button named \"{button}\"."
            : $"The ribbon's selected tab has {buttons.Count} buttons named \"{button}\".");
        if (only.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("On a static page the ribbon's buttons do nothing; render the page "
                + "interactively to use them.");
        }

        if (Choices.IsDisabled(only))
        {
            throw new InvalidOperationException($"The ribbon button \"{button}\" is disabled.");
        }

        await only.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Follows these key tips in turn, as pressing Alt and then each key does: a tab's selects the tab, and
    /// a button's clicks it, in the selected tab or beside the tabs. Each call starts from Alt. bUnit runs no
    /// script, so no badge shows, a group's key tip opens nothing, and a field's moves no focus. Throws for a key
    /// tip the ribbon doesn't have at that point, for a key after the one that chose a command, for a disabled
    /// control, and on a static page, where key tips don't work.</summary>
    public async Task PressKeyTipsAsync(params string[] keyTips)
    {
        var root = _find();
        if (root.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("On a static page the ribbon has no key tips; render the page "
                + "interactively to use them.");
        }

        var inTab = false;
        var ended = false;
        foreach (var keyTip in keyTips)
        {
            if (ended)
            {
                throw new InvalidOperationException(
                    $"Key tips end once a command is chosen, so \"{keyTip}\" has nothing to reach.");
            }

            var showing = inTab ? Own(Commands?.QuerySelectorAll("[data-key-tip]")) : BesideTheCommands();
            var control = showing.FirstOrDefault(found => string.Equals(found.GetAttribute("data-key-tip"),
                    keyTip, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(inTab
                    ? $"The ribbon's selected tab has no key tip \"{keyTip}\"."
                    : $"The ribbon has no key tip \"{keyTip}\" on its tabs or beside them.");
            if (Choices.IsDisabled(control))
            {
                throw new InvalidOperationException($"The ribbon control with the key tip \"{keyTip}\" is disabled.");
            }

            // A tab's key and a collapsed group's leave the key tips showing, and any other key ends them.
            var isTab = control.GetAttribute("role") == "tab";
            inTab |= isTab;
            ended = !isTab && control.GetAttribute("data-part") != "group-button";
            // An element around a field has nothing to click.
            if (control.LocalName == "button")
            {
                await control.ClickAsync(new MouseEventArgs { Detail = 1 });
            }
        }
    }

    // The controls with key tips in the tabs' row: the tabs, and what sits before and after them.
    private IEnumerable<IElement> BesideTheCommands()
    {
        var root = _find();
        return Own(root.QuerySelectorAll("[data-key-tip]"))
            .Where(control => control.Closest("[role=tabpanel]") is not { } panel || !root.Contains(panel));
    }

    // What is inside a ribbon that is itself inside this one, as in a dialog written in a group, isn't this ribbon's.
    private IEnumerable<IElement> Own(IEnumerable<IElement>? found)
    {
        var root = _find();
        return (found ?? []).Where(element => element.Closest(".tb-ribbon") == root);
    }

    // The selected tab's row of commands: the other tabs' rows stay in the markup, hidden, once they have been shown.
    // The path starts at the ribbon, so a tab panel around the ribbon isn't taken for one of its own.
    private IElement? Commands => _find()
        .QuerySelector(":scope > .tb-tabs > [role=tabpanel]:not([hidden]) > [data-part=commands]");
}

/// <summary>A <c>TbTooltip</c>, found by its trigger's name.</summary>
public sealed class TbTooltipDriver
{
    private readonly Func<IElement> _find;

    internal TbTooltipDriver(Func<IElement> find) => _find = find;

    /// <summary>The tooltip's text. bUnit's markup holds it from the start, so nothing has to show it first.</summary>
    public string Text => _find().QuerySelector(":scope > [role=tooltip]") is { } bubble
        ? Notices.Collapsed(bubble.TextContent)
        : throw new InvalidOperationException("This tooltip shows nothing: it's disabled or has no text.");
}

/// <summary>A <c>TbPopconfirm</c>, found by its trigger's name.</summary>
public sealed class TbPopconfirmDriver
{
    private readonly string _trigger;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbPopconfirmDriver(string trigger, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_trigger, _findAll) = (trigger, findAll);

    /// <summary>Presses the confirm button, which raises <c>OnConfirm</c>. In bUnit the button works without
    /// opening the panel first. A disabled popconfirm throws, since a user can't open it.</summary>
    public async Task ConfirmAsync() => await PressAsync("confirm");

    /// <summary>Presses the cancel button, which raises <c>OnCancel</c>. A disabled popconfirm throws.</summary>
    public async Task CancelAsync() => await PressAsync("cancel");

    private Task PressAsync(string part)
    {
        var trigger = PopoverTriggers.Find(_findAll, _trigger, "popconfirm", refuseDisabled: true);
        var button = _findAll($"[id='{trigger.GetAttribute("aria-controls")}'] [data-part={part}]").SingleOrDefault()
            ?? throw new InvalidOperationException($"The popover that \"{_trigger}\" opens isn't a popconfirm.");
        return button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>A <c>TbTextField</c> or <c>TbCodeField</c>, found by its label.</summary>
public sealed class TbTextFieldDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbTextFieldDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The text the field shows.</summary>
    public string? Text => _find().GetAttribute("value");

    /// <summary>Types this text into the field and commits it, as typing and then leaving the field does. It sends
    /// whichever of the input and change events the field listens for, so a mask or check runs as in a browser. A
    /// read-only or disabled field throws, since a user can't type into it.</summary>
    public async Task EnterAsync(string text)
    {
        Typing.ThrowIfLocked(_find(), "text");
        if (_find().HasAttribute("blazor:oninput"))
        {
            await _find().InputAsync(new ChangeEventArgs { Value = text });
        }

        if (_find().HasAttribute("blazor:onchange"))
        {
            await _find().ChangeAsync(new ChangeEventArgs { Value = text });
        }

        await LeaveAsync();
    }

    /// <summary>Whether a <c>Revealable</c> password field shows its password.</summary>
    public bool PasswordShown => RevealButton()?.GetAttribute("aria-pressed") == "true";

    /// <summary>Shows the password with the field's show-password button, as a user does, unless it shows already. A
    /// field that isn't <c>Revealable</c>, or is read-only or disabled, throws.</summary>
    public async Task ShowPasswordAsync() => await RevealAsync(true);

    /// <summary>Hides the password with the field's show-password button, unless it's hidden already, and throws as
    /// <see cref="ShowPasswordAsync"/> does.</summary>
    public async Task HidePasswordAsync() => await RevealAsync(false);

    private IElement? RevealButton() => _find().Closest(".tb-field")?.QuerySelector("[data-part=reveal]");

    private async Task RevealAsync(bool shown)
    {
        var button = RevealButton() ?? throw new InvalidOperationException(
            "The field has no show-password button; make it Revealable, with Type set to Password.");
        if (button.HasAttribute("disabled"))
        {
            throw new InvalidOperationException("The field is read-only or disabled, so its password stays hidden.");
        }

        if (PasswordShown != shown)
        {
            await button.ClickAsync(new MouseEventArgs { Detail = 1 });
        }
    }
}

/// <summary>A <c>TbCheckbox</c> or <c>TbSwitch</c>, found by its label.</summary>
public sealed class TbCheckboxDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbCheckboxDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>Whether it's checked, or on for a switch.</summary>
    public bool Checked => _find().HasAttribute("checked");

    /// <summary>Whether a <c>bool?</c> checkbox is mixed, for a null value; on a static page the box itself doesn't
    /// show it.</summary>
    public bool Indeterminate => _find().Closest("[data-commits]")?.HasAttribute("data-indeterminate") == true;

    /// <summary>Checks it, as clicking it does, unless it's checked already. A read-only or disabled one throws, and so
    /// does one on a static page, which posts with its form.</summary>
    public async Task CheckAsync() => await SetAsync(true);

    /// <summary>Unchecks it, as clicking it does, unless it's unchecked already, and throws as
    /// <see cref="CheckAsync"/> does.</summary>
    public async Task UncheckAsync() => await SetAsync(false);

    private Task SetAsync(bool on)
    {
        var box = _find();
        Choices.ThrowIfLocked(box);
        return Checked == on ? Task.CompletedTask : box.ChangeAsync(new ChangeEventArgs { Value = on });
    }
}

/// <summary>A <c>TbCheckboxGroup</c>, found by its label. Its options are named by their text.</summary>
public sealed class TbCheckboxGroupDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbCheckboxGroupDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The checked options' names, in order.</summary>
    public IReadOnlyList<string> Checked => [.. Options().Where(option => option.Input.HasAttribute("checked"))
        .Select(option => option.Name)];

    /// <summary>Checks the options with these names, as clicking them does, leaving the others as they are. A
    /// disabled group or option throws, and so does a group on a static page.</summary>
    public async Task CheckAsync(params string[] options) => await SetAsync(options, on: true);

    /// <summary>Unchecks the options with these names, leaving the others as they are, and throws as
    /// <see cref="CheckAsync"/> does.</summary>
    public async Task UncheckAsync(params string[] options) => await SetAsync(options, on: false);

    private async Task SetAsync(string[] names, bool on)
    {
        ArgumentOutOfRangeException.ThrowIfZero(names.Length, nameof(names));
        foreach (var name in names)
        {
            var input = Option(name);
            Choices.ThrowIfLocked(input);
            if (input.HasAttribute("checked") != on)
            {
                await input.ChangeAsync(new ChangeEventArgs { Value = on });
            }
        }
    }

    private IElement Option(string name) => Options().Where(option => option.Name == name).ToList() is [var option]
        ? option.Input : throw new InvalidOperationException($"The group has no option named \"{name}\".");

    private IEnumerable<(string Name, IElement Input)> Options() => _find()
        .QuerySelectorAll("[data-part=options] input[data-part=input]")
        .Select(input => (Choices.NameOf(input.ParentElement!.QuerySelector(":scope > [data-part=label]")!), input));
}

/// <summary>A <c>TbRadioGroup</c>, found by its label. Its options are named by their text.</summary>
public sealed class TbRadioGroupDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbRadioGroupDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The chosen option's name, or null when none is chosen.</summary>
    public string? Chosen => Options().FirstOrDefault(option => option.Input.HasAttribute("checked")).Name;

    /// <summary>Chooses the option with this name, as clicking it does. A read-only or disabled group or option
    /// throws, and so does a group on a static page.</summary>
    public async Task ChooseAsync(string option)
    {
        var input = Options().Where(each => each.Name == option).ToList() is [var found] ? found.Input
            : throw new InvalidOperationException($"The group has no option named \"{option}\".");
        if (_find().GetAttribute("aria-readonly") == "true")
        {
            throw new InvalidOperationException("This group is read-only, so its options can't be chosen.");
        }

        Choices.ThrowIfLocked(input);
        if (!input.HasAttribute("checked"))
        {
            await input.ChangeAsync(new ChangeEventArgs());
        }
    }

    private IEnumerable<(string Name, IElement Input)> Options() => _find()
        .QuerySelectorAll("[data-part=options] input[type=radio]")
        .Select(input => (Choices.NameOf(input.ParentElement!.QuerySelector(":scope > [data-part=label]")!), input));
}

/// <summary>A <c>TbRating</c>, found by its label.</summary>
public sealed class TbRatingDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbRatingDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>How many stars are chosen, or 0 when none are.</summary>
    public int Value => Stars().FirstOrDefault(star => star.HasAttribute("checked")) is { } chosen
        ? int.Parse(chosen.GetAttribute("value")!, CultureInfo.InvariantCulture) : 0;

    /// <summary>How many stars there are.</summary>
    public int Max => Stars().Count;

    /// <summary>Chooses this many stars, as clicking the last of them does, unless they're chosen already. A
    /// read-only or disabled rating throws, and so does one on a static page.</summary>
    public async Task RateAsync(int stars)
    {
        var all = Stars();
        ArgumentOutOfRangeException.ThrowIfLessThan(stars, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(stars, all.Count);
        ThrowIfReadOnly();
        Choices.ThrowIfLocked(all[stars - 1]);
        if (!all[stars - 1].HasAttribute("checked"))
        {
            await all[stars - 1].ChangeAsync(new ChangeEventArgs());
        }
    }

    /// <summary>Takes the rating back to no stars, as clicking the chosen star does, unless none are chosen. A
    /// rating that isn't <c>Clearable</c> throws, and so does a read-only or disabled one, or one on a static page.
    /// </summary>
    public async Task ClearAsync()
    {
        if (!_find().HasAttribute("data-clearable"))
        {
            throw new InvalidOperationException("This rating isn't Clearable, so its stars can't be taken back.");
        }

        ThrowIfReadOnly();
        // Past the read-only check, a star is locked only by a disabled fieldset or a static page.
        Choices.ThrowIfLocked(Stars()[0]);
        if (Stars().FirstOrDefault(star => star.HasAttribute("checked")) is { } chosen)
        {
            await chosen.ClickAsync(new MouseEventArgs());
        }
    }

    private void ThrowIfReadOnly()
    {
        if (_find().GetAttribute("aria-readonly") == "true")
        {
            throw new InvalidOperationException("This rating is read-only, so its stars can't be changed.");
        }
    }

    private List<IElement> Stars() => [.. _find().QuerySelectorAll("[data-part=stars] input[type=radio]")];
}

/// <summary>A <c>TbToggleGroup</c> or <c>TbMultiToggleGroup</c>, found by its label, or by its <c>AriaLabel</c>
/// when it has no label. Its items are named by their text, or by their <c>AriaLabel</c> when they show only an icon.
/// </summary>
public sealed class TbToggleGroupDriver
{
    private readonly Func<IElement> _find;

    internal TbToggleGroupDriver(Func<IElement> find) => _find = find;

    /// <summary>The pressed items' names, in order.</summary>
    public IReadOnlyList<string> Chosen => [.. Items().Where(item => item.GetAttribute("aria-pressed") == "true")
        .Select(Choices.NameOf)];

    /// <summary>Presses the items with these names that aren't pressed yet, as clicking them does. Give a
    /// <c>TbToggleGroup</c> just one, since it keeps one item pressed. A disabled item throws, and so does a group on a
    /// static page.</summary>
    public async Task ChooseAsync(params string[] items) => await SetAsync(items, pressed: true);

    /// <summary>Releases the pressed items with these names by pressing them again, which a <c>TbMultiToggleGroup</c>
    /// or a <c>Clearable</c> <c>TbToggleGroup</c> allows, and throws as <see cref="ChooseAsync"/> does.</summary>
    public async Task UnchooseAsync(params string[] items) => await SetAsync(items, pressed: false);

    private async Task SetAsync(string[] names, bool pressed)
    {
        ArgumentOutOfRangeException.ThrowIfZero(names.Length, nameof(names));
        var group = _find();
        if (group.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("A toggle group on a static page can't be pressed; render it on an "
                + "interactive page.");
        }

        if (pressed && names.Length > 1 && !group.HasAttribute("data-multiple"))
        {
            throw new ArgumentException("A TbToggleGroup has one item pressed at a time, so choose just one.",
                nameof(names));
        }

        if (!pressed && !group.HasAttribute("data-clearable"))
        {
            throw new InvalidOperationException("This TbToggleGroup can't release its pressed item; make it Clearable "
                + "to allow that.");
        }

        foreach (var name in names)
        {
            var item = Items().Where(each => Choices.NameOf(each) == name).ToList() is [var found] ? found
                : throw new InvalidOperationException($"The toggle group has no item named \"{name}\".");
            if (item.HasAttribute("disabled") || item.GetAttribute("aria-disabled") == "true")
            {
                throw new InvalidOperationException($"\"{name}\" is disabled.");
            }

            if ((item.GetAttribute("aria-pressed") == "true") != pressed)
            {
                await item.ClickAsync(new MouseEventArgs { Detail = 1 });
            }
        }
    }

    private IEnumerable<IElement> Items() => _find().QuerySelectorAll("[data-part=item]");
}

/// <summary>A <c>TbList</c>, found by its <c>aria-label</c>. Its items are named by their text, leaving out their
/// description and end content.</summary>
public sealed class TbListDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbListDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>The items' names, in order.</summary>
    public IReadOnlyList<string> Items => [.. ItemElements().Select(NameOf)];

    /// <summary>Clicks the item with this text. The item must be a button or a link, which it is when it has an
    /// <c>OnClick</c> or an <c>Href</c>; a link also takes the test's <c>NavigationManager</c> to its address. A
    /// disabled item throws.</summary>
    public async Task ClickAsync(string item)
    {
        var found = ItemElements().Where(each => NameOf(each) == item).ToList() is [var one] ? one
            : throw new InvalidOperationException($"The list has no item that reads \"{item}\".");
        var action = found.QuerySelector(":scope > [data-part=action]");
        if (action is null || action.HasAttribute("disabled"))
        {
            throw new InvalidOperationException(action is not null || found.QuerySelector(
                ":scope > [data-part=content][aria-disabled=true]") is not null ? $"\"{item}\" is disabled."
                : $"\"{item}\" is neither a button nor a link; give it an OnClick or an Href.");
        }

        await action.ClickAsync(new MouseEventArgs { Detail = 1 });
        _scripts.Follow(action);
    }

    private IEnumerable<IElement> ItemElements() => _find().Children.Where(child => child.LocalName == "li");

    private static string NameOf(IElement item) => item.QuerySelector("[data-part=text]")?.TextContent.Trim() ?? "";
}

/// <summary>A <c>TbNumberField</c>, found by its label.</summary>
public sealed class TbNumberFieldDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbNumberFieldDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The text the field shows.</summary>
    public string? Text => _find().GetAttribute("value");

    /// <summary>Types this text into the field and commits it, as typing and then leaving the field does. Text that
    /// isn't a number shows the field's error, as in a browser. A read-only or disabled field throws, since a user
    /// can't type into it.</summary>
    public async Task EnterAsync(string text)
    {
        // A plain number box reads what was typed through its own event; a formatted one is a text box.
        var input = _find();
        Typing.ThrowIfLocked(input, "a number");
        await (input.GetAttribute("role") == "spinbutton" ? input.ChangeAsync(new ChangeEventArgs { Value = text })
            : input.TriggerEventAsync("ontbnumberchange", new NumberChangeEventArgs { Value = text }));
        await LeaveAsync();
    }

    /// <summary>Clicks the field's increase button this many times, or its decrease button for a negative count.
    /// Stepping stops at <c>Min</c> or <c>Max</c>, where the button turns off, as in a browser. A read-only or disabled
    /// field throws, and so does one with <c>HideStepButtons</c>; type into that one with <see cref="EnterAsync"/>.
    /// </summary>
    public async Task StepAsync(int count)
    {
        ArgumentOutOfRangeException.ThrowIfZero(count);
        var part = count < 0 ? "decrement" : "increment";
        if (Locks.IsLocked(_find()))
        {
            throw new InvalidOperationException("This field is read-only or disabled, so it can't be stepped.");
        }

        for (var step = 0; step < Math.Abs(count); step++)
        {
            var button = _find().Closest("[data-part=control]")?.QuerySelector($"[data-part={part}]")
                ?? throw new InvalidOperationException("This field hides its step buttons; use EnterAsync instead.");
            await button.ClickAsync(new MouseEventArgs());
        }
    }
}

/// <summary>A <c>TbSlider</c>, found by its label.</summary>
public sealed class TbSliderDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbSliderDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The value the field holds.</summary>
    public decimal Value => SliderThumb.ValueOf(_find());

    /// <summary>Moves the slider to this value, as dragging its thumb does. A value between steps lands on the nearest
    /// step, and one past an end on that end, as in a browser. On a static page the slider is a native one that posts
    /// with its form, so this throws, as it does for a disabled or read-only slider.</summary>
    public Task SetAsync(decimal value) => SliderThumb.MoveAsync(_find, value);
}

/// <summary>A <c>TbDockManager</c>, found by its accessible name, or the only one in the markup. Its panels are
/// named by their titles and its separators by their names, such as "Resize Trails".</summary>
public sealed class TbDockDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbDockDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts) =>
        (_find, _findAll, _scripts) = (find, findAll, scripts);

    /// <summary>The helpers inside the panel with this title, such as
    /// <c>cut.Tb().Dock().Panel("Notes").TextField("Title")</c>, so a field that several panels have can be told
    /// apart. Throws when the panel's content isn't in the markup, as happens until the panel first shows.</summary>
    public TrailblazorMarkup Panel(string panel) =>
        new TrailblazorMarkup(_findAll, _scripts).Within(() => PanelElement(panel));

    /// <summary>The panel with this title, to assert on what it shows. Throws when the panel's content isn't in the
    /// markup, as <see cref="Panel"/> does.</summary>
    public IElement PanelElement(string panel) =>
        PoppedPanels().SingleOrDefault(each => each.GetAttribute("aria-label") == panel)
        ?? (Tab(panel).GetAttribute("aria-controls") is { } id
            ? _find().Children.Single(child => child.Id == id)
            : throw new InvalidOperationException($"The content of the panel \"{panel}\" isn't in the markup; select "
                + "the panel first."));

    /// <summary>The titles of every panel's tab, group by group in reading order.</summary>
    public IReadOnlyList<string> Panels => Tabs().Select(Title).ToList();

    /// <summary>The titles of the panels that show, one for each group, each floating window and the auto-hidden
    /// panel that is slid out, in reading order.
    /// While a group fills the dock, its panel shows and each window's.</summary>
    public IReadOnlyList<string> Showing =>
        Tabs().Where(tab => tab.GetAttribute("aria-selected") == "true" && !Covered(tab)).Select(Title).ToList();

    /// <summary>Selects the panel with this title, as clicking its tab does. Throws for a panel behind the group
    /// that fills the dock, as every helper that clicks does.</summary>
    public async Task SelectAsync(string panel)
    {
        await SlidOutAsync(panel);
        await Reachable(panel).ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Closes the panel with this title, as clicking the close mark on its tab does, so the panel's
    /// <c>CanClose</c> is asked and can keep it open. Throws for a panel that can't be closed.</summary>
    public async Task CloseAsync(string panel)
    {
        await SlidOutAsync(panel);
        await (Reachable(panel).QuerySelector("[data-part=close]")
            ?? throw new InvalidOperationException($"The panel \"{panel}\" can't be closed."))
        .ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Moves the tab of the panel with this title along its row by this many places, forward when the
    /// number is positive and back when it's negative, as "Move tab forward" and "Move tab back" in its group's menu
    /// do. The menu acts on the group's showing panel, so each move helper selects the panel first. Throws when the
    /// tab reaches the start or end of its row before it has moved that far.</summary>
    public async Task MoveTabAsync(string panel, int places)
    {
        for (var moved = 0; moved < Math.Abs(places); moved++)
        {
            await ChooseAsync(panel, places > 0 ? "[data-move=forward]" : "[data-move=back]",
                $"The tab of \"{panel}\" is at the {(places > 0 ? "end" : "start")} of its row.");
        }
    }

    /// <summary>Moves the panel with this title to the end of the group with this number, counted from 1 in reading
    /// order, and shows it there, as "Move to group" in its group's menu does. Throws for the panel's own group or a
    /// number the dock doesn't have.</summary>
    public async Task MoveToGroupAsync(string panel, int group) =>
        await ChooseAsync(panel, $"[data-move-group='{group.ToString(CultureInfo.InvariantCulture)}']",
            $"The dock has no other group {group} to move \"{panel}\" to.");

    /// <summary>Gives the panel with this title a new group on this side of its own, sharing the space the group
    /// had, as "New group beside this one" in its group's menu does. Throws for a panel that is alone in its group,
    /// for a compact dock, and for <c>Before</c> or <c>After</c>.</summary>
    public async Task MoveToNewGroupAsync(string panel, DockPosition side) =>
        await ChooseAsync(panel, $"[data-move-beside={Side(side, nameof(side))}]", IsCompact ? Compact
            : $"The panel \"{panel}\" is alone in its group, so it has no group to split.");

    /// <summary>Gives the panel with this title a new group along this edge of the dock, as "New group at the dock's
    /// edge" in its group's menu does. Throws when it's the only panel docked, for a compact dock, and for
    /// <c>Before</c> or <c>After</c>.</summary>
    public async Task MoveToEdgeAsync(string panel, DockPosition edge) =>
        await ChooseAsync(panel, $"[data-move-edge={Side(edge, nameof(edge))}]", IsCompact ? Compact
            : $"The panel \"{panel}\" is the only one docked, so it has nowhere to move.");

    /// <summary>Whether the dock is compact: told that it is narrower than its <c>CompactBelow</c> width, with its
    /// docked panels showing as the tabs of one group in place of the groups its layout puts side by side.</summary>
    public bool IsCompact => _find().HasAttribute("data-compact");

    /// <summary>Whether the dock is <c>Locked</c>, so that its menus offer no moves and its tabs don't drag. For a
    /// locked dock the helpers that move, float, dock, auto-hide or pop out a panel throw; selecting, closing,
    /// maximizing, restoring and resizing still work.</summary>
    public bool IsLocked => _find().HasAttribute("data-locked");

    /// <summary>The title of the dock's <c>ActivePanel</c>, the showing panel of the group that last had the focus or
    /// a press, or null before any has, or while that panel is in a window of its own. bUnit runs no script, so a
    /// press or the focus doesn't change it there; selecting another tab of its group, closing it and the app's own
    /// <c>ActivePanel</c> do.</summary>
    public string? ActivePanel => _find().GetAttribute("data-active-panel") is { } id
        ? Tabs().Concat(StripButtons()).Where(tab => tab.GetAttribute("data-panel") == id).Select(Title)
            .FirstOrDefault()
        : null;

    private const string Locked = "The dock is Locked, so its menus offer no moves: set Locked to false first.";

    /// <summary>Tells the dock that it is narrower than its <c>CompactBelow</c> width or, with false, that it is
    /// wide enough again. On a page the dock's script does this from the dock's own width. A narrow dock whose
    /// layout puts groups side by side becomes compact, unless its <c>CompactBelow</c> is <c>Breakpoint.Xs</c>.
    /// Throws for a dock that shows no tab, since the helper finds the dock's script by one.</summary>
    public async Task NarrowAsync(bool narrow = true)
    {
        var tab = Tabs().FirstOrDefault() ?? throw new InvalidOperationException(
            "The dock shows no tab, and the helper finds the dock's script by one; open a panel first.");
        var report = _scripts.Dock(tab.Id!);
        await _scripts.InvokeAsync(() => report.OnNarrow(narrow, null, false));
    }

    private const string Compact = "The dock is compact: it was told it is narrower than its CompactBelow width and "
        + "shows one group, so a panel can't get a new group. Call NarrowAsync(false), or set a smaller CompactBelow, "
        + "such as Breakpoint.Xs.";

    /// <summary>The titles of the panels in floating windows, window by window, oldest window first.</summary>
    public IReadOnlyList<string> Floating => Tabs().Where(tab => IsWindow(tab.Closest("[data-part=group]")!))
        .Select(Title).ToList();

    /// <summary>The titles of the panels put away on the strips along the dock's edges: the top strip's, then those
    /// of the start, the end and the bottom strips.</summary>
    public IReadOnlyList<string> AutoHidden => StripButtons().Select(Title).ToList();

    /// <summary>Puts the panel with this title away on the strip along this edge of the dock, as "Auto-hide" in its
    /// group's menu does. Throws for a panel that is on that strip already, and for <c>Before</c> or <c>After</c>.
    /// </summary>
    public async Task AutoHideAsync(string panel, DockPosition edge) =>
        await ChooseAsync(panel, $"[data-move-hide={Side(edge, nameof(edge))}]",
            $"The panel \"{panel}\" is on that strip already.");

    /// <summary>Slides the auto-hidden panel with this title out over the dock, as its button on its strip does. A
    /// panel that is out already stays out. Throws for a panel that isn't on a strip.</summary>
    public async Task RevealAsync(string panel)
    {
        var button = StripButtons().SingleOrDefault(each => Title(each) == panel)
            ?? throw new InvalidOperationException($"The panel \"{panel}\" isn't on a strip.");
        if (button.GetAttribute("aria-expanded") != "true")
        {
            await button.ClickAsync(new MouseEventArgs { Detail = 1 });
        }
    }

    /// <summary>Slides the slid-out panel back to its strip, as Escape in it does. Throws when no panel is slid out.
    /// </summary>
    public async Task ConcealAsync()
    {
        var tab = Tabs().FirstOrDefault(each => each.Closest("[data-part=group]")!.HasAttribute("data-edge"))
            ?? throw new InvalidOperationException("No auto-hidden panel is slid out.");
        var report = _scripts.Dock(tab.Id!);
        await _scripts.InvokeAsync(() => report.OnConceal(true));
    }

    /// <summary>Takes the panel with this title out into a floating window of its own, as "Float" in its group's
    /// menu does. Throws for a panel that has a window to itself already.</summary>
    public async Task FloatAsync(string panel) =>
        await ChooseAsync(panel, "[data-move=float]", $"The panel \"{panel}\" floats already.");

    /// <summary>Docks the floating panel with this title as the showing tab of the largest group, as "Dock" in its
    /// window's menu does. Throws for a panel that is docked already.</summary>
    public async Task DockAsync(string panel) =>
        await ChooseAsync(panel, "[data-move=dock]", $"The panel \"{panel}\" is docked already.");

    /// <summary>Opens the panel with this title in a browser window of its own, as "Open in new window" in its
    /// group's menu does, and gives the panel. bUnit has no windows, so the panel stays in the markup, drawn as it is
    /// in its window: without a tab, and with its Return button. Throws when the dock's <c>AllowPopOut</c> is off,
    /// or the panel's.</summary>
    public async Task<IElement> PopOutAsync(string panel)
    {
        await ChooseAsync(panel, "[data-move=pop-out]", $"The panel \"{panel}\" can't open in a window of its "
            + "own: the dock's AllowPopOut is off, or the panel's is.");
        return PanelElement(panel);
    }

    /// <summary>The titles of the panels that show in browser windows of their own.</summary>
    public IReadOnlyList<string> PoppedOut => [.. PoppedPanels().Select(panel => panel.GetAttribute("aria-label")!)];

    /// <summary>Brings the panel with this title back to the dock from its own browser window, as that window's
    /// Return button does. Throws for a panel that isn't in a window of its own.</summary>
    public async Task ReturnAsync(string panel)
    {
        var button = PoppedPanels().SingleOrDefault(each => each.GetAttribute("aria-label") == panel)
            ?.QuerySelector("[data-part=return]")
            ?? throw new InvalidOperationException($"The panel \"{panel}\" isn't in a browser window of its own.");
        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    // The panels drawn as they are in browser windows of their own, where they have no tab to be found by.
    private IEnumerable<IElement> PoppedPanels() =>
        _find().Children.Where(child => child.HasAttribute("data-popped"));

    /// <summary>The box of the floating window that holds the panel with this title, in percent of the dock: where
    /// it starts, from the side where text starts and from the top, and its width and height. Throws for a panel
    /// that isn't in a window.</summary>
    public (double X, double Y, double Width, double Height) Window(string panel)
    {
        var style = WindowRow(panel).GetAttribute("style")!;
        double Number(string name) => double.Parse(
            style.Split($"--tb-dock-{name}: ")[1].Split(';')[0], CultureInfo.InvariantCulture);
        return (Number("x"), Number("y"), Number("w"), Number("h"));
    }

    /// <summary>Moves the window that holds the panel with this title by this many percent of the dock, to the right
    /// and down when the numbers are positive, as the arrow keys on its Move button do. The window stays inside the
    /// dock. bUnit has no layout, so it takes text to run from left to right. Throws for a panel that isn't in a
    /// window.</summary>
    public async Task MoveWindowAsync(string panel, int right, int down)
    {
        var (x, y, width, height) = Window(panel);
        await ReportWindowAsync(panel, x + right, y + down, width, height, false);
    }

    /// <summary>Resizes the window that holds the panel with this title by this many percent of the dock, wider and
    /// taller when the numbers are positive, as the arrow keys on its grip do. The window stays inside the dock and
    /// keeps a tenth of the dock's width and height; bUnit has no pixels, so <c>MinGroupSize</c> isn't applied.
    /// Throws for a panel that isn't in a window.</summary>
    public async Task ResizeWindowAsync(string panel, int wider, int taller)
    {
        var (x, y, width, height) = Window(panel);
        await ReportWindowAsync(panel, x, y, Math.Clamp(width + wider, 10, 100 - x),
            Math.Clamp(height + taller, 10, 100 - y), true);
    }

    /// <summary>Shows the panel with this title and has its group fill the dock, as "Maximize" in its group's menu
    /// does. Throws when the group is the dock's only one, or fills the dock already.</summary>
    public async Task MaximizeAsync(string panel) => await ChooseAsync(panel, "[data-move=maximize]",
        $"The group of \"{panel}\" is the dock's only one, or fills the dock already.", moves: false);

    /// <summary>Shows every group again, as the Restore button of the group that fills the dock does. Throws when
    /// no group fills the dock.</summary>
    public async Task RestoreAsync() =>
        await (_find().Children.FirstOrDefault(child => child.HasAttribute("data-maximized"))
                ?.QuerySelector("[data-part=restore]")
            ?? throw new InvalidOperationException("No group fills the dock."))
        .ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Opens the menu of the tab of the panel with this title, as a right-click does, and clicks the item
    /// with this name, through the submenus named before it, as in
    /// <c>ChooseFromTabMenuAsync("Notes", "Move to group", "Group 2: Map")</c>. The menu acts on that panel whether or
    /// not it shows. Throws for an item the menu doesn't have.</summary>
    public async Task ChooseFromTabMenuAsync(string panel, params string[] path)
    {
        await SlidOutAsync(panel);
        await new TbContextMenuDriver(() => Tab(panel).Closest(".tb-context-menu")!, _scripts,
            Reachable(panel).GetAttribute("data-panel")!).ChooseAsync(path);
    }

    /// <summary>The share, in percent, that the part before the separator with this name has of the space it and
    /// the part after it take together. For a slid-out panel's inner edge, which is named like a separator, as in
    /// "Resize Notes", it is the share of the dock the panel takes.</summary>
    public int Size(string separator) =>
        int.Parse(Separator(separator).GetAttribute("aria-valuenow")!, CultureInfo.InvariantCulture);

    /// <summary>Moves the separator with this name, as its keys do, until the part before it has this percent of the
    /// space it and the part after it take together. bUnit has no pixels, so <c>MinGroupSize</c> isn't applied; a
    /// percent below 1 or above 99 throws. For a slid-out panel's inner edge, the percent is the share of the dock the
    /// panel takes, and one below 10 or above 90 throws.</summary>
    public async Task ResizeAsync(string separator, int percent)
    {
        var bar = Separator(separator);
        var slides = bar.GetAttribute("data-part") == "slide-separator";
        var (least, most) = slides ? (10, 90) : (1, 99);
        if (percent < least || percent > most)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent,
                slides ? "A slid-out panel takes from 10% to 90% of the dock." : "A separator moves from 1% to 99%.");
        }

        // The keys aren't pressed for a separator already there, so nothing is reported.
        if (percent == Size(separator))
        {
            return;
        }

        var report = _scripts.Dock(Tabs().First().Id!);
        var number = int.Parse(_find().GetAttribute("data-report") ?? "0", CultureInfo.InvariantCulture) + 1;
        if (slides)
        {
            // The edge of the panel that is slid out, whose tab is the one in the row at that edge.
            var panel = Tabs().First(tab => tab.Closest("[data-part=group]")!.HasAttribute("data-edge"))
                .GetAttribute("data-panel")!;
            await _scripts.InvokeAsync(() => report.OnSlide(panel, percent, number));
            return;
        }

        var index = int.Parse(bar.GetAttribute("data-index")!, CultureInfo.InvariantCulture);
        await _scripts.InvokeAsync(() => report.OnResize(index, percent / 100.0, number));
    }

    // Clicks an item of the menu of the panel's group, which acts on the group's showing panel.
    private async Task ChooseAsync(string panel, string item, string missing, bool moves = true)
    {
        if (moves && IsLocked)
        {
            throw new InvalidOperationException(Locked);
        }

        await SlidOutAsync(panel);
        if (Reachable(panel).GetAttribute("aria-selected") != "true")
        {
            await SelectAsync(panel);
        }

        var menu = Tab(panel).Closest("[data-part=group]")!.Children
            .SingleOrDefault(child => child.GetAttribute("data-part") == "group-menu")
            ?? throw new InvalidOperationException(IsLocked ? missing
                : "The dock is on a static page, where it has no menus; render it on an interactive page.");
        var found = menu.QuerySelector(item);
        if (found is null || found.GetAttribute("aria-disabled") == "true")
        {
            throw new InvalidOperationException(missing);
        }

        await found.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private static string Side(DockPosition side, string parameter) =>
        side is DockPosition.Before or DockPosition.After
            ? throw new ArgumentOutOfRangeException(parameter, side, "Use Start, End, Top or Bottom.")
            : side.ToString().ToLowerInvariant();

    // The dock's own tabs, not those of tabs inside a panel, by their groups' numbers: bUnit has the rows as they
    // were first rendered, and a window can be older than a docked group.
    private IEnumerable<IElement> Tabs() => _find().Children
        .Where(child => child.GetAttribute("data-part") == "group")
        .OrderBy(group => int.Parse(group.GetAttribute("data-group")!, CultureInfo.InvariantCulture))
        .SelectMany(group => group.QuerySelectorAll("[role=tab]"));

    private IElement Tab(string panel) => Tabs().SingleOrDefault(tab => Title(tab) == panel)
        ?? throw new InvalidOperationException(StripButtons().Any(button => Title(button) == panel)
            ? $"The panel \"{panel}\" is on a strip; slide it out with RevealAsync first."
            : $"No panel is titled \"{panel}\".");

    private IElement WindowRow(string panel) => !StripButtons().Any(button => Title(button) == panel)
        && Tab(panel).Closest("[data-part=group]") is { } row && IsWindow(row) ? row
            : throw new InvalidOperationException($"The panel \"{panel}\" isn't in a floating window.");

    // A slid-out panel is drawn as a window, but it isn't one.
    private static bool IsWindow(IElement row) => row.HasAttribute("data-floating") && !row.HasAttribute("data-edge");

    // The buttons of the strips, which lie beside the dock's area in its frame.
    private IEnumerable<IElement> StripButtons() => _find().ParentElement!.Children
        .Where(child => child.GetAttribute("data-part") == "strip").SelectMany(strip => strip.Children);

    // A panel on a strip has no tab until it slides out, so a helper that needs its tab slides it out first.
    private async Task SlidOutAsync(string panel)
    {
        if (StripButtons().Any(each => Title(each) == panel))
        {
            await RevealAsync(panel);
        }
    }

    // Tells the dock what its script would after a move or a resize of a window.
    private async Task ReportWindowAsync(string panel, double x, double y, double width, double height, bool resized)
    {
        var id = Tab(panel).GetAttribute("data-panel")!;
        var report = _scripts.Dock(Tabs().First().Id!);
        var number = int.Parse(_find().GetAttribute("data-report") ?? "0", CultureInfo.InvariantCulture) + 1;
        await _scripts.InvokeAsync(() => report.OnFloat(id, x, y, width, height, resized, number));
    }

    // On a page, a tab behind the group that fills the dock can't be clicked.
    private IElement Reachable(string panel) => Tab(panel) is var tab && !Covered(tab) ? tab
        : throw new InvalidOperationException(
            $"The panel \"{panel}\" is behind the group that fills the dock; restore the groups first.");

    private static bool Covered(IElement tab) =>
        tab.Closest("[data-part=group]")!.ClassList.Contains("tb-dock__group--covered");

    private IElement Separator(string separator) => _find().Children.SingleOrDefault(child =>
        child.GetAttribute("data-part") is "separator" or "slide-separator"
        && child.GetAttribute("aria-label") == separator)
            ?? throw new InvalidOperationException($"No separator is named \"{separator}\".");

    private static string Title(IElement tab) => tab.QuerySelector("[data-part=title]")!.TextContent;
}

/// <summary>The <c>TbSplitter</c> separator that resizes one pane, found by its name, such as "Resize Trails".
/// </summary>
public sealed class TbSplitterDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbSplitterDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>The pane's size in percent, as the separator shows it.</summary>
    public int Size => int.Parse(_find().GetAttribute("aria-valuenow")!, CultureInfo.InvariantCulture);

    /// <summary>Resizes the pane to this percent, as the separator's keys do: the pane after it gives or takes the
    /// difference, within both panes' limits. A size the limits don't allow throws, and so does a collapsed pane on
    /// either side, since the keys can't resize those.</summary>
    public async Task ResizeAsync(int percent)
    {
        // The keys aren't pressed for a pane already at this size, so nothing is reported.
        if (percent == Size)
        {
            return;
        }

        var separator = _find();
        var bar = separator.ParentElement!;
        var root = bar.ParentElement!;
        var panes = root.Children.Where(child => child.GetAttribute("data-part") == "pane").ToList();
        var before = panes.IndexOf(bar.PreviousElementSibling!);
        var (first, second) = (panes[before], panes[before + 1]);
        if (first.HasAttribute("data-collapsed") || second.HasAttribute("data-collapsed"))
        {
            throw new InvalidOperationException(
                "The pane on one side of this separator is collapsed, and the keys can't resize it; restore it first.");
        }

        // As in the script: each pane's share follows its flex-grow, and the pair moves within both panes' limits.
        var grows = panes.Select(Grow).ToList();
        var total = grows.Sum();
        var shares = grows.Select(grow => total > 0 ? grow / total * 100 : 0).ToArray();
        var pair = shares[before] + shares[before + 1];
        var moved = Math.Clamp(shares[before] + percent - Size, Limit(first, "data-min"), Limit(first, "data-max"));
        moved = pair - Math.Clamp(pair - moved, Limit(second, "data-min"), Limit(second, "data-max"));
        if (Math.Round(moved) != percent)
        {
            throw new InvalidOperationException(
                $"The panes' limits keep this pane from {percent}%; the nearest it reaches is {Math.Round(moved)}%.");
        }

        shares[before] = Math.Max(moved, 0);
        shares[before + 1] = pair - shares[before];
        var report = _scripts.Splitter(first.Id!);
        var number = int.Parse(root.GetAttribute("data-report") ?? "0", CultureInfo.InvariantCulture) + 1;
        await _scripts.InvokeAsync(() => report.OnResize([.. panes.Select(pane => pane.Id!)],
            [.. shares.Select(share => Math.Round(share, 2))], number));
    }

    /// <summary>Whether the pane that Enter on the separator collapses and restores is collapsed. That's the pane
    /// before the separator, or the one after it when the one before can't collapse; but while the pane before is
    /// open, a collapsed last pane after it comes first, so the pane can change after a toggle. Throws when neither
    /// pane can collapse.</summary>
    public bool Collapsed => ToggledPane().HasAttribute("data-collapsed");

    /// <summary>Collapses the pane that Enter on the separator collapses, if it's open, as pressing Enter does, which
    /// runs the app's <c>CollapsedChanged</c>. Throws when neither pane beside the separator can collapse.</summary>
    public Task CollapseAsync() => SetCollapsedAsync(true);

    /// <summary>Restores the pane that Enter on the separator restores, if it's collapsed, as
    /// <see cref="CollapseAsync"/> collapses it.</summary>
    public Task RestoreAsync() => SetCollapsedAsync(false);

    private IElement ToggledPane()
    {
        var separator = _find();
        var id = separator.GetAttribute("data-toggles") ?? throw new InvalidOperationException(
            $"Neither pane beside \"{separator.GetAttribute("aria-label")}\" can collapse.");
        return separator.ParentElement!.ParentElement!.Children.Single(child => child.Id == id);
    }

    // On Enter the script reports the pane before the separator, and the splitter picks which pane to toggle.
    private async Task SetCollapsedAsync(bool collapsed)
    {
        if (Collapsed == collapsed)
        {
            return;
        }

        var before = _find().ParentElement!.PreviousElementSibling!.Id!;
        var report = _scripts.Splitter(before);
        await _scripts.InvokeAsync(() => report.OnToggle(before));
    }

    private static double Grow(IElement pane) => double.Parse(
        pane.GetAttribute("style")!.Split(';')[0].Split(':')[1], CultureInfo.InvariantCulture);

    private static double Limit(IElement pane, string name) =>
        double.Parse(pane.GetAttribute(name)!, CultureInfo.InvariantCulture);
}

/// <summary>A <c>TbRangeSlider</c>, found by its label.</summary>
public sealed class TbRangeSliderDriver : TbFieldDriver
{
    private readonly Func<IElement> _start;

    internal TbRangeSliderDriver(Func<IElement> start) : base(start) => _start = start;

    /// <summary>The start and end the field holds.</summary>
    public (decimal Start, decimal End) Range => (SliderThumb.ValueOf(_start()), SliderThumb.ValueOf(End()));

    /// <summary>Moves both thumbs, as dragging each does, landing each on the nearest step between the slider's
    /// minimum and maximum. The thumbs can't pass each other, so the end moves first when the new start lands past the
    /// old end. A start after the end throws, and so does a static page or a disabled or read-only slider.</summary>
    public async Task SetAsync(decimal start, decimal end)
    {
        if (start > end)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, $"The start can't be after the end, {end}.");
        }

        if (SliderThumb.Snap(_start(), start) > Range.End)
        {
            await SliderThumb.MoveAsync(End, end);
            await SliderThumb.MoveAsync(_start, start);
            return;
        }

        await SliderThumb.MoveAsync(_start, start);
        await SliderThumb.MoveAsync(End, end);
    }

    private IElement End() => _start().Closest(".tb-slider--range")!.QuerySelector("[data-part=end-thumb]")!;
}

internal static class SliderThumb
{
    internal static decimal ValueOf(IElement input) =>
        decimal.Parse(input.GetAttribute("data-value")!, CultureInfo.InvariantCulture);

    // Input then change, as a drag and its release do; the input event re-renders the thumb, so it's found again.
    internal static async Task MoveAsync(Func<IElement> find, decimal value)
    {
        var input = find();
        Choices.ThrowIfLocked(input);
        var text = Snap(input, value).ToString("0.############################", CultureInfo.InvariantCulture);
        await input.InputAsync(new ChangeEventArgs { Value = text });
        await find().ChangeAsync(new ChangeEventArgs { Value = text });
    }

    // A browser puts a range input's value on the step nearest it, counted from the minimum, within the range.
    internal static decimal Snap(IElement input, decimal value)
    {
        var (min, max, step) = (Attribute(input, "min"), Attribute(input, "max"), Attribute(input, "step"));
        var steps = Math.Floor((Math.Clamp(value, min, max) - min) / step + 0.5m);
        return min + Math.Min(steps, Math.Floor((max - min) / step)) * step;
    }

    private static decimal Attribute(IElement input, string name) =>
        decimal.Parse(input.GetAttribute(name)!, CultureInfo.InvariantCulture);
}

/// <summary>A <c>TbCascader</c>, found by its label. Its choices are named by their text.</summary>
public sealed class TbCascaderDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbCascaderDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll) : base(find) =>
        (_find, _findAll) = (find, findAll);

    /// <summary>The text the field shows: the chosen path, or what was typed.</summary>
    public string? Text => _find().GetAttribute("value");

    /// <summary>The paths that match what was typed, as the field shows them.</summary>
    public IReadOnlyList<string> Results => [.. Popup().QuerySelectorAll("[data-part=results] [data-part=option]")
        .Select(Choices.NameOf)];

    /// <summary>Chooses a path a level at a time, as a user clicks through the lists: the choice with each text in
    /// turn, such as <c>ChooseAsync("Canada", "Ontario", "Toronto")</c>. Throws when a level has no such choice, or
    /// it is disabled.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        Usable();
        Choices.ThrowIfLocked(_find());
        for (var level = 0; level < path.Length; level++)
        {
            var choice = Popup().QuerySelectorAll($"[data-part=column][data-level='{level}'] [data-part=option]")
                .FirstOrDefault(option => option.QuerySelector(".tb-cascader__text")?.TextContent == path[level])
                ?? throw new InvalidOperationException($"Level {level + 1} has no choice \"{path[level]}\".");
            if (choice.GetAttribute("aria-disabled") == "true")
            {
                throw new InvalidOperationException($"\"{path[level]}\" is disabled.");
            }

            await choice.ClickAsync(new MouseEventArgs { Detail = 1 });
        }
    }

    /// <summary>Types the text, as a user does, which searches every level for paths that hold it; read them with
    /// <see cref="Results"/>.</summary>
    public async Task SearchAsync(string text)
    {
        Usable();
        Choices.ThrowIfLocked(_find());
        await _find().InputAsync(new ChangeEventArgs { Value = text });
    }

    /// <summary>Types the text, then chooses the matching path shown as <paramref name="result"/>, such as
    /// <c>"Canada / Ontario / Toronto"</c>. Throws when no such path matches.</summary>
    public async Task ChooseResultAsync(string text, string result)
    {
        await SearchAsync(text);
        await Choices.Named([.. Popup().QuerySelectorAll("[data-part=results] [data-part=option]")], result)
            .ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private void Usable()
    {
        var input = _find();
        if (input.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("A cascader on a static page is a plain input that posts with its "
                + "form; render it on an interactive page to choose.");
        }

    }

    private IElement Popup() => _findAll($"#{_find().GetAttribute("aria-controls")}").Single();
}

/// <summary>A <c>TbAutocomplete</c>, found by its label.</summary>
public sealed class TbAutocompleteDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbAutocompleteDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll,
        ScriptCalls scripts) : base(find) => (_find, _findAll, _scripts) = (find, findAll, scripts);

    /// <summary>The text the field shows.</summary>
    public string? Text => _find().GetAttribute("value");

    /// <summary>The names of the suggestions in the field's list, in order, from its last search; they stay after a
    /// choice closes the list.</summary>
    public IReadOnlyList<string> Suggestions => [.. Options().Select(Choices.NameOf)];

    /// <summary>Types the text, as a user does, and waits for the suggestions, without choosing one. It doesn't wait
    /// for <c>OnTextChanged</c>, which may wait on the app's clock. It throws as <see cref="ChooseAsync"/> does.
    /// </summary>
    public async Task TypeAsync(string text) => await TypeAndSearchAsync(text);

    /// <summary>Types the text, as a user does, waits for the suggestions, and chooses the option with this name. On a
    /// static page the field is a plain input that posts with its form, so this throws, as it does for a disabled or
    /// read-only field.</summary>
    public async Task ChooseAsync(string text, string option)
    {
        var typing = await TypeAndSearchAsync(text);
        // Click even when the field already holds this option, since the click puts its text back over what was typed.
        await Choices.Named(Options().ToList(), option).ClickAsync(new MouseEventArgs { Detail = 1 });
        await typing;
    }

    /// <summary>Types the text, as a user does, waits for the suggestions, and leaves the field without choosing one,
    /// which commits the text: a suggestion it matches is taken; failing that, a <c>FreeText</c> field takes the text
    /// itself, and any other field puts back its value's text. It throws as <see cref="ChooseAsync"/> does.</summary>
    public async Task EnterAsync(string text)
    {
        var typing = await TypeAndSearchAsync(text);
        // OnTextChanged may still wait on the app's clock; committing ends that wait, so await the typing after it.
        await _find().ChangeAsync(new ChangeEventArgs { Value = text });
        await LeaveAsync();
        await typing;
    }

    /// <summary>Clears the field with its clear button, as a user does. A field that isn't <c>Clearable</c>, shows no
    /// text, or is read-only or disabled has no clear button, so this throws, as it does on a static page.</summary>
    public async Task ClearAsync()
    {
        if (_find().HasAttribute("data-static"))
        {
            throw new InvalidOperationException("An autocomplete on a static page is a plain input that posts with its "
                + "form; render it on an interactive page to clear it.");
        }

        var clear = _find().Closest(".tb-field")?.QuerySelector("[data-part=clear]") ?? throw new
            InvalidOperationException("The field has no clear button; make it Clearable, give it some text, and leave "
                + "it editable.");
        await clear.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    // Returns the typing, which may finish only after the search, since OnTextChanged may wait on the app's clock.
    private async Task<Task> TypeAndSearchAsync(string text)
    {
        var input = _find();
        Typing.ThrowIfLocked(input, "a search");
        if (input.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("An autocomplete on a static page is a plain input that posts with its "
                + "form; render it on an interactive page to type into it.");
        }

        var typing = input.InputAsync(new ChangeEventArgs { Value = text });
        await _scripts.WaitForAsync(() => typing.IsCompleted || (_find().GetAttribute("value") == text
            && _findAll($"[id='{_find().GetAttribute("aria-controls")}'][aria-busy]").Count == 0));
        return typing;
    }

    private IEnumerable<IElement> Options() =>
        _findAll($"[id='{_find().GetAttribute("aria-controls")}'] [role=option][data-part=option]");
}

/// <summary>A <c>TbMentionInput</c>, found by its label.</summary>
public sealed class TbMentionInputDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbMentionInputDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll,
        ScriptCalls scripts) : base(find) => (_find, _findAll, _scripts) = (find, findAll, scripts);

    /// <summary>The text the field shows.</summary>
    public string? Text => _find().GetAttribute("value");

    /// <summary>The names of the suggestions for the mention being typed, in order.</summary>
    public IReadOnlyList<string> Suggestions => [.. Options().Select(Choices.NameOf)];

    /// <summary>Types this text, as a user does with the caret at its end. When it ends with a trigger and the start of
    /// a mention, such as <c>"Thanks @an"</c>, this waits for the suggestions. A read-only or disabled field throws.
    /// </summary>
    public async Task TypeAsync(string text)
    {
        Typing.ThrowIfLocked(_find(), "text");
        await _find().InputAsync(new ChangeEventArgs { Value = text });
        var query = QueryAt(text);
        var events = _scripts.MentionInput(_find());
        var number = ResultsFor() + 1;
        await _scripts.InvokeAsync(() => events.OnQuery(query?.Trigger, query?.Text ?? "", number));
    }

    /// <summary>Chooses the suggestion with this name for the mention being typed, as clicking it does: the trigger and
    /// the text after it become the trigger, the suggestion's text and a space. Type the start of the mention first
    /// with <see cref="TypeAsync"/>.</summary>
    public async Task ChooseAsync(string suggestion)
    {
        var text = Text ?? "";
        var query = QueryAt(text) ?? throw new InvalidOperationException(
            "No mention is being typed; type a trigger and the start of the mention with TypeAsync first.");
        var options = Options().ToList();
        var option = Choices.Named(options, suggestion);
        var events = _scripts.MentionInput(_find());
        var resultsFor = ResultsFor();
        await _scripts.InvokeAsync(() => events.OnChosen(options.IndexOf(option), resultsFor));
        var start = text.Length - query.Text.Length - query.Trigger.Length;
        await _find().InputAsync(new ChangeEventArgs { Value = $"{text[..start]}{option.GetAttribute("data-insert")} " });
    }

    // The number of the query the suggestions answer, which the script numbers as it asks.
    private int ResultsFor() => int.Parse(_findAll($"[id='{_find().GetAttribute("aria-controls")}']")[0]
        .GetAttribute("data-results-for")!, CultureInfo.InvariantCulture);

    private MentionQuery? QueryAt(string text) =>
        Typing.MentionAt(text, _find().GetAttribute("data-triggers") ?? "@");

    private IEnumerable<IElement> Options() =>
        _findAll($"[id='{_find().GetAttribute("aria-controls")}'] [role=option][data-part=option]");
}

/// <summary>A <c>TbColorPicker</c>, found by its label.</summary>
public sealed partial class TbColorPickerDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbColorPickerDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The color the field holds, as it was given or last committed, such as <c>#rrggbb</c>, or <c>null</c>
    /// when it holds none. Text typed but not yet committed doesn't count. On a static page, the field is a plain input,
    /// so this reads the color it was rendered with.</summary>
    public string? Value
    {
        get
        {
            var input = _find();
            if (input.HasAttribute("data-static") || input.HasAttribute("data-prerender"))
            {
                return input.GetAttribute("value") is { Length: > 0 } text ? text : null;
            }

            return input.GetAttribute("data-value");
        }
    }

    /// <summary>Types the color, as <c>#rrggbb</c>, or <c>#rrggbbaa</c> for a field with <c>Alpha</c>, and commits it,
    /// as typing it and pressing Enter does. On a static page the field is a plain input that posts with its form, so
    /// this throws, as it does for a disabled or read-only field.</summary>
    public async Task ChooseAsync(string color)
    {
        if (!HexColor().IsMatch(color))
        {
            throw new ArgumentException($"Pass the color as #rrggbb or #rrggbbaa, not {color}.", nameof(color));
        }

        var input = _find();
        Typing.ThrowIfLocked(input, "a color");
        if (input.HasAttribute("data-static") || input.HasAttribute("data-prerender"))
        {
            throw new InvalidOperationException("A color picker on a static page is a plain input that posts with its "
                + "form; render it on an interactive page to choose a color.");
        }

        await input.ChangeAsync(new ChangeEventArgs { Value = color });
    }

    [System.Text.RegularExpressions.GeneratedRegex("^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")]
    private static partial System.Text.RegularExpressions.Regex HexColor();
}

/// <summary>A <c>TbFileUpload</c>, found by its label.</summary>
public sealed class TbFileUploadDriver : TbFieldDriver
{
    // The types a browser gives the files a test is most likely to pick; any other extension gets an empty type.
    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".avif"] = "image/avif", [".bmp"] = "image/bmp", [".csv"] = "text/csv", [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".gif"] = "image/gif", [".html"] = "text/html", [".jpeg"] = "image/jpeg", [".jpg"] = "image/jpeg",
        [".json"] = "application/json", [".mov"] = "video/quicktime", [".mp3"] = "audio/mpeg", [".mp4"] = "video/mp4",
        [".ogg"] = "audio/ogg", [".pdf"] = "application/pdf", [".png"] = "image/png",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".svg"] = "image/svg+xml", [".txt"] = "text/plain", [".wav"] = "audio/wav", [".webm"] = "video/webm",
        [".webp"] = "image/webp", [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [".xml"] = "text/xml",
        [".zip"] = "application/zip",
    };

    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbFileUploadDriver(Func<IElement> find, ScriptCalls scripts) : base(find) => (_find, _scripts) = (find, scripts);

    /// <summary>Chooses the files at these paths, as dropping them or picking them in the file dialog does. A file
    /// with a common extension takes the type a browser gives it, such as <c>image/png</c> for a <c>.png</c>, and any
    /// other an empty type; to set the type yourself, pass <c>InputFileContent</c>s instead.
    /// On a static page the field is a native one that posts with its form, so this throws, as it does for a disabled
    /// or read-only field.</summary>
    public async Task ChooseAsync(params string[] paths)
    {
        ArgumentOutOfRangeException.ThrowIfZero(paths.Length, nameof(paths));
        Upload().UploadFiles(FromPaths(paths));
    }

    /// <summary>Chooses files made in the test, such as <c>InputFileContent.CreateFromText("Day one", "notes.txt")</c>,
    /// as dropping them or picking them in the file dialog does. Throws as the paths overload does.</summary>
    public async Task ChooseAsync(params InputFileContent[] files)
    {
        ArgumentOutOfRangeException.ThrowIfZero(files.Length, nameof(files));
        Upload().UploadFiles(files);
    }

    /// <summary>The names of the files the field lists as chosen, in order. A field with <c>Hidden</c> set lists none.
    /// </summary>
    public IReadOnlyList<string> Files =>
        [.. FieldRoot().QuerySelectorAll("[data-part=file]").Select(file => file.GetAttribute("data-name")!)];

    /// <summary>Where the listed file with this name stands: <c>Chosen</c> for a field without <c>Upload</c>. Throws for
    /// a name the field doesn't list.</summary>
    public FileUploadStatus Status(string file) => Listed(file).GetAttribute("data-status") is { } status
        ? Enum.Parse<FileUploadStatus>(status, ignoreCase: true) : FileUploadStatus.Chosen;

    /// <summary>Waits until no file is being sent or waiting its turn, so the app's <c>Upload</c> has finished with each.
    /// </summary>
    public async Task WaitForUploadsAsync()
    {
        // Found first, so a field that isn't there fails as any helper does rather than as a wait that runs out.
        FieldRoot();
        await _scripts.WaitForAsync(() => !FieldRoot().HasAttribute("data-uploading"));
    }

    /// <summary>Sends the chosen files that haven't been sent, as the Upload button does when <c>AutoUpload</c> is
    /// off. Throws when the field shows no Upload button, or it is off.</summary>
    public async Task UploadAsync() => await PressAsync(FieldRoot().QuerySelector("[data-part=upload]"),
        "The field has no enabled Upload button: it needs Upload set, AutoUpload off and a file not yet sent.");

    /// <summary>Stops the upload of the listed file with this name, as its cancel button does. Throws when that file
    /// isn't being sent.</summary>
    public async Task CancelAsync(string file) => await PressAsync(Listed(file).QuerySelector("[data-part=cancel]"),
        $"\"{file}\" isn't being sent.");

    /// <summary>Sends the listed file with this name again, as its retry button does. Throws when that file's upload
    /// hasn't failed or been canceled.</summary>
    public async Task RetryAsync(string file) => await PressAsync(Listed(file).QuerySelector("[data-part=retry]"),
        $"\"{file}\" hasn't failed or been canceled.");

    private IElement Listed(string file) =>
        FieldRoot().QuerySelectorAll("[data-part=file]").FirstOrDefault(item => item.GetAttribute("data-name") == file)
        ?? throw new InvalidOperationException($"The field lists no file named \"{file}\".");

    private static async Task PressAsync(IElement? button, string missing)
    {
        if (button is null || button.HasAttribute("disabled"))
        {
            throw new InvalidOperationException(missing);
        }

        await button.ClickAsync(new MouseEventArgs());
    }

    // The files at these paths, each with the type a browser gives its extension.
    internal static InputFileContent[] FromPaths(string[] paths) => [.. paths.Select(path =>
        InputFileContent.CreateFromBinary(File.ReadAllBytes(path), Path.GetFileName(path), File.GetLastWriteTimeUtc(path),
            Types.GetValueOrDefault(Path.GetExtension(path), "")))];

    private IElement FieldRoot() => _find().Closest(".tb-file-upload")!;

    private IRenderedComponent<InputFile> Upload()
    {
        var input = _find();
        Choices.ThrowIfLocked(input);
        if (input.HasAttribute("data-prerender"))
        {
            throw new InvalidOperationException("This file upload is prerendered; render it on an interactive page to "
                + "choose files.");
        }

        return _scripts.InputFile(input.Id!);
    }
}

/// <summary>An open <c>TbLightbox</c>, found by its title.</summary>
public sealed class TbLightboxDriver
{
    private readonly TbDialogDriver _dialog;

    internal TbLightboxDriver(TbDialogDriver dialog) => _dialog = dialog;

    /// <summary>The shown image's position, from 0.</summary>
    public int Index => int.Parse(Part("frame").GetAttribute("data-index")!, CultureInfo.InvariantCulture);

    /// <summary>The shown image's alt text.</summary>
    public string Alt => Part("image").GetAttribute("alt")!;

    /// <summary>The shown image's caption, or null when it has none.</summary>
    public string? Caption => Find().QuerySelector("[data-part=caption]")?.TextContent.Trim();

    /// <summary>The zoom, as a multiple of the size that fits the image to the window: 1 until zoomed in.</summary>
    public double Zoom => double.Parse(Part("stage").GetAttribute("data-zoom")!, CultureInfo.InvariantCulture);

    /// <summary>How far the image is turned clockwise, in degrees: 0, 90, 180 or 270.</summary>
    public int Rotation => 90 * int.Parse(Part("stage").GetAttribute("data-turns")!, CultureInfo.InvariantCulture);

    /// <summary>Shows the next image, as pressing Next does. On the last image of a lightbox that doesn't loop, Next
    /// is off, so this throws.</summary>
    public Task NextAsync() => PressAsync("next");

    /// <summary>Shows the previous image, as pressing Previous does. On the first image of a lightbox that doesn't
    /// loop, Previous is off, so this throws.</summary>
    public Task PreviousAsync() => PressAsync("previous");

    /// <summary>Zooms in one step, as pressing Zoom in does. At the closest zoom it's off, so this throws.</summary>
    public Task ZoomInAsync() => PressAsync("zoom-in");

    /// <summary>Zooms out one step, as pressing Zoom out does. At the fitted size it's off, so this throws.</summary>
    public Task ZoomOutAsync() => PressAsync("zoom-out");

    /// <summary>Turns the image a quarter turn counterclockwise, as pressing Rotate left does.</summary>
    public Task RotateLeftAsync() => PressAsync("rotate-left");

    /// <summary>Turns the image a quarter turn clockwise, as pressing Rotate right does.</summary>
    public Task RotateRightAsync() => PressAsync("rotate-right");

    /// <summary>Undoes zoom and rotation, as pressing Fit to window does. With neither, it's off, so this throws.
    /// </summary>
    public Task FitAsync() => PressAsync("fit");

    /// <summary>Closes the lightbox with Escape, as a keyboard user does, and waits for the app's <c>OpenChanged</c>
    /// to finish.</summary>
    public Task CloseAsync() => _dialog.CancelAsync();

    private async Task PressAsync(string part)
    {
        var button = Part(part);
        if (button.GetAttribute("aria-disabled") == "true" || button.HasAttribute("disabled"))
        {
            throw new InvalidOperationException($"The lightbox's {part} button is off right now, so it can't be pressed.");
        }

        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private IElement Part(string part) => Find().QuerySelector($"[data-part={part}]")
        ?? throw new InvalidOperationException($"The lightbox has no {part}; it shows only one image, or none.");

    private IElement Find() => _dialog.Find() is var dialog && dialog.ClassList.Contains("tb-lightbox") ? dialog
        : throw new InvalidOperationException("The open dialog isn't a lightbox; give the lightbox's title.");
}

/// <summary>An open dialog, found by its title: a <c>TbDialog</c> or <c>TbDrawer</c>, or one from
/// <c>IDialogService</c> such as a confirm.</summary>
public sealed class TbDialogDriver
{
    private readonly string? _title;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbDialogDriver(string? title, Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts) =>
        (_title, _findAll, _scripts) = (title, findAll, scripts);

    /// <summary>Returns the open dialog, without waiting, to find what it shows. A dialog on a static page opens and
    /// closes in the browser, so this throws for one, as it does when no dialog with the title is open.</summary>
    public async Task<IElement> WaitForAsync() => Find();

    /// <summary>Waits until the app has closed the dialog, as it does once a save it started has finished. Returns at
    /// once when the dialog isn't open.</summary>
    public async Task WaitForClosedAsync() => await _scripts.WaitForAsync(() => !IsOpen);

    /// <summary>The dialog's title, which names it, read from the open dialog.</summary>
    public string Title => TitleOf(Find())!;

    /// <summary>Whether the dialog is open. A dialog on a static page opens and closes in the browser, so this throws
    /// for one.</summary>
    public bool IsOpen => Matching().Count > 0;

    /// <summary>Whether the dialog shows at its own size, minimized to its header or maximized to fill the window.
    /// </summary>
    public DialogWindowState WindowState => _scripts.Dialog(Find()).Dialog.WindowState;

    /// <summary>Minimizes the dialog with its Minimize button, unless it's minimized already. Throws for a dialog
    /// without the button, which shows only when the dialog is <c>Minimizable</c>.</summary>
    public Task MinimizeAsync() => SetWindowStateAsync(DialogWindowState.Minimized, "minimize");

    /// <summary>Maximizes the dialog with its Maximize button, unless it's maximized already. Throws for a dialog
    /// without the button, which shows only when the dialog is <c>Maximizable</c>.</summary>
    public Task MaximizeAsync() => SetWindowStateAsync(DialogWindowState.Maximized, "maximize");

    /// <summary>Brings a minimized or maximized dialog back to its own size with its Restore button. Does nothing for
    /// a dialog at its own size.</summary>
    public async Task RestoreAsync() => await SetWindowStateAsync(DialogWindowState.Normal,
        WindowState == DialogWindowState.Minimized ? "minimize" : "maximize");

    /// <summary>Clicks the dialog's button with this name, such as a confirm's <c>Delete</c> or a form's <c>Save</c>.
    /// When the click closes the dialog, this waits for a <c>TbDialog</c>'s or <c>TbDrawer</c>'s <c>OpenChanged</c> to
    /// finish.</summary>
    public async Task ChooseAsync(string button)
    {
        var dialog = _scripts.Dialog(Find()).Dialog;
        var buttons = Find().QuerySelectorAll("button").Where(found => Choices.NameOf(found) == button).ToList();
        if (buttons is not [var only])
        {
            throw new InvalidOperationException($"Expected one button named \"{button}\" in the dialog, found "
                + $"{buttons.Count}.");
        }

        var answered = _scripts.AnsweredDialogs;
        await only.ClickAsync(new MouseEventArgs { Detail = 1 });
        await AnsweredAsync(dialog, answered);
    }

    /// <summary>Dismisses the dialog with Escape, as a keyboard user does, and waits for a <c>TbDialog</c>'s or
    /// <c>TbDrawer</c>'s <c>OpenChanged</c> to finish. This throws for a dialog whose options don't let Escape dismiss
    /// it.</summary>
    public async Task CancelAsync()
    {
        var frame = _scripts.Dialog(Find());
        if (frame.Dialog.Options.DismissOnEscape != true)
        {
            throw new InvalidOperationException("This dialog's options don't let Escape dismiss it.");
        }

        var answered = _scripts.AnsweredDialogs;
        await _scripts.InvokeAsync(() => frame.Dismiss(DialogDismissReason.Escape));
        await AnsweredAsync(frame.Dialog, answered);
    }

    // The header's Minimize and Maximize buttons each toggle between their state and the dialog's own size.
    private async Task SetWindowStateAsync(DialogWindowState state, string part)
    {
        if (WindowState == state)
        {
            return;
        }

        var button = Find().QuerySelector($"[data-part=header] [data-part={part}]")
            ?? throw new InvalidOperationException($"This dialog has no {char.ToUpperInvariant(part[0])}{part[1..]} "
                + "button.");
        var dialog = _scripts.Dialog(Find()).Dialog;
        var counted = dialog.WindowStateCommits;
        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
        await _scripts.WaitForAsync(() => dialog.WindowStateCommits > counted);
    }

    // A dialog that stays open, or that its content kept open, has no answer to wait for.
    private Task AnsweredAsync(DialogReference dialog, int answered) =>
        dialog.IsClosing ? _scripts.WaitForAnswerAsync(answered) : Task.CompletedTask;

    internal IElement Find()
    {
        var dialogs = Matching();
        return dialogs.Count == 1 ? dialogs[0] : throw new InvalidOperationException(dialogs.Count == 0
            ? _title is null ? "No dialog is in the markup." : $"No dialog titled \"{_title}\" is in the markup."
            : _title is null ? $"{dialogs.Count} dialogs are open; give the title of the one to use."
            : $"{dialogs.Count} dialogs are titled \"{_title}\"; give each its own title.");
    }

    // A static page's dialog has the browser open and close it.
    private List<IElement> Matching()
    {
        var dialogs = _findAll("dialog[data-part=dialog]")
            .Where(dialog => _title is null || TitleOf(dialog) == _title).ToList();
        if (dialogs.Any(dialog => dialog.HasAttribute("closedby")))
        {
            throw new InvalidOperationException("A dialog on a static page opens and closes in the browser; test it in "
                + "Playwright.");
        }

        return dialogs;
    }

    private string? TitleOf(IElement dialog) =>
        _findAll($"[id='{dialog.GetAttribute("aria-labelledby")}']").SingleOrDefault()?.TextContent.Trim();
}

/// <summary>A <c>TbTagInput</c>, found by its label.</summary>
public sealed class TbTagInputDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbTagInputDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The tags the field shows, in order.</summary>
    public IReadOnlyList<string> Tags => [.. TagElements().Select(TextOf)];

    /// <summary>Adds this text as a tag, through the same event the field's script raises on Enter, so a duplicate,
    /// a tag past the limit or one the field's check refuses shows its message, as in a browser. A disabled or
    /// read-only field throws, since the user can't type in it.</summary>
    public async Task AddAsync(string text)
    {
        var input = _find();
        if (Locks.IsLocked(input))
        {
            throw new InvalidOperationException("This tag input is disabled or read-only, so tags can't be added.");
        }

        await input.TriggerEventAsync("ontbtagadd", new TagAddEventArgs { Text = text });
    }

    /// <summary>Removes this tag with its remove button, as a click does. A tag the field doesn't show, or one it
    /// shows without a remove button because the field is disabled or read-only, throws.</summary>
    public async Task RemoveAsync(string tag)
    {
        var button = TagElements().FirstOrDefault(element => TextOf(element) == tag)
                ?.QuerySelector("[data-part=remove]")
            ?? throw new InvalidOperationException($"The tag input has no tag \"{tag}\" that can be removed.");
        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private static string TextOf(IElement tag) => tag.QuerySelector(".tb-chip__label")!.TextContent.Trim();

    private IEnumerable<IElement> TagElements() =>
        _find().Closest("[data-part=control]")?.QuerySelectorAll("[data-part=tag]") ?? Enumerable.Empty<IElement>();
}

/// <summary>A <c>TbSignaturePad</c>, found by its label.</summary>
public sealed class TbSignaturePadDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbSignaturePadDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>Whether something is drawn or a name is typed.</summary>
    public bool IsSigned => _find().QuerySelector("[data-part=surface] > svg:not([data-part=draft]) > path") is not null
        || !string.IsNullOrWhiteSpace(_find().QuerySelector("[data-part=typed-name] input")?.GetAttribute("value"));

    /// <summary>Draws a stroke through these points, in the signature's space of 600 by 200, as a pointer pressed on the
    /// first and lifted on the last would. Throws when the pad is switched to typing a name.</summary>
    public async Task DrawAsync(params (double X, double Y)[] points) =>
        await (_find().QuerySelector("[data-part=surface]")
            ?? throw new InvalidOperationException("The signature pad is set to typing a name, so it can't be drawn on."))
            .TriggerEventAsync("ontbsignaturestroke", new SignatureStrokeEventArgs
            {
                Points = [.. points.SelectMany(point => new[] { point.X, point.Y })],
            });

    /// <summary>Clicks Undo, which takes back the last stroke. Throws when there's nothing to take back or the pad is
    /// disabled or read-only.</summary>
    public async Task UndoAsync() => await Button("undo").ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Clicks Clear. Throws when there's nothing to clear or the pad is disabled or read-only.</summary>
    public async Task ClearAsync() => await Button("clear").ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Switches to typing a name if the pad isn't already, and types this one, which the pad takes as the
    /// field changes.</summary>
    public async Task TypeNameAsync(string name)
    {
        if (_find().QuerySelector("[data-part=type-name]")?.GetAttribute("aria-pressed") != "true")
        {
            await Button("type-name").ClickAsync(new MouseEventArgs { Detail = 1 });
        }

        await _find().QuerySelector("[data-part=typed-name] input")!.ChangeAsync(new ChangeEventArgs { Value = name });
    }

    private IElement Button(string part) =>
        _find().QuerySelector($"[data-part={part}]") is { } button && !button.HasAttribute("disabled")
            ? button : throw new InvalidOperationException($"The signature pad's {part} button is disabled.");
}

/// <summary>A <c>TbCalendar</c>, found by its label.</summary>
public sealed class TbCalendarDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbCalendarDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>Picks this date as a user would: up to the years, over to its decade, then down through its year and
    /// month to the day. A disabled or read-only calendar, or a date it can't choose, throws.</summary>
    public async Task ChooseAsync(DateOnly date)
    {
        var grid = Grid();
        if (grid.GetAttribute("aria-disabled") == "true" || grid.GetAttribute("aria-readonly") == "true")
        {
            throw new InvalidOperationException("This calendar is disabled or read-only, so no date can be chosen.");
        }

        while (Grid().GetAttribute("data-view") != "years")
        {
            await ClickAsync(_find().QuerySelector("[data-part=title]")!);
        }

        var year = $"{date.Year:0000}-01-01";
        while (Cell(year) is null)
        {
            var shown = int.Parse(Grid().QuerySelector("[data-date]")!.GetAttribute("data-date")![..4],
                CultureInfo.InvariantCulture);
            await ClickAsync(_find().QuerySelector(shown > date.Year ? "[data-part=previous]" : "[data-part=next]")!);
        }

        await ClickCellAsync(year, date);
        await ClickCellAsync($"{date.Year:0000}-{date.Month:00}-01", date);
        await ClickCellAsync(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), date);
    }

    private IElement Grid() => _find().QuerySelector("[role=grid]")
        ?? throw new InvalidOperationException("This calendar isn't interactive, so it shows no grid to pick from.");

    private IElement? Cell(string iso) => Grid().QuerySelector($"[data-date='{iso}']");

    private Task ClickCellAsync(string iso, DateOnly date) =>
        Cell(iso) is { } cell && cell.GetAttribute("aria-disabled") != "true" ? ClickAsync(cell)
            : throw new InvalidOperationException($"The calendar can't choose {date:yyyy-MM-dd}.");

    private static Task ClickAsync(IElement button) => button.ClickAsync(new MouseEventArgs { Detail = 1 });
}

/// <summary>A <c>TbRichTextEditor</c>, found by its label.</summary>
public sealed class TbRichTextEditorDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbRichTextEditorDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll,
        ScriptCalls scripts) : base(find) => (_find, _findAll, _scripts) = (find, findAll, scripts);

    /// <summary>Replaces the editor's text with this HTML and commits it, through the same event the editor's script
    /// raises in a browser, so the value keeps only the formats the editor allows. A disabled or read-only editor
    /// throws, since the user can't change it.</summary>
    public async Task EnterAsync(string html) =>
        await Unlocked().TriggerEventAsync("ontbrichtextchange", new RichTextChangeEventArgs { Html = html });

    private IElement Unlocked()
    {
        var editor = _find();
        if (editor.GetAttribute("aria-disabled") == "true" || editor.GetAttribute("aria-readonly") == "true")
        {
            throw new InvalidOperationException("This editor is disabled or read-only, so its text can't be changed.");
        }

        return editor;
    }

    /// <summary>Mentions an item, as typing this text into an empty editor and choosing the suggestion with this
    /// name does. The text must end with a trigger and the start of the mention, such as <c>"Thanks @ad"</c>. The
    /// app's search runs for it, and the editor's text becomes the text before the trigger, the mention and a
    /// space. A disabled or read-only editor throws.</summary>
    public async Task MentionAsync(string typed, string suggestion)
    {
        var editor = Unlocked();
        var events = _scripts.RichTextMentions(editor);
        var query = Typing.MentionAt(typed, editor.GetAttribute("data-triggers") ?? "")
            ?? throw new InvalidOperationException($"\"{typed}\" doesn't end with a mention being typed: one of "
                + "the editor's triggers at the start of a word, with no space after it.");
        var list = $"[id='{editor.GetAttribute("aria-controls")}']";
        var number = int.Parse(_findAll(list)[0].GetAttribute("data-results-for")!, CultureInfo.InvariantCulture) + 1;
        await _scripts.InvokeAsync(() => events.OnQuery(query.Trigger, query.Text, number));
        var options = _findAll($"{list} [role=option][data-part=option]").ToList();
        var option = Choices.Named(options, suggestion);
        var item = new MentionItem(option.GetAttribute("data-insert")![query.Trigger.Length..])
        {
            Id = option.GetAttribute("data-mention-id"),
        };
        await _scripts.InvokeAsync(() => events.OnChosen(options.IndexOf(option), number));
        var before = typed[..^(query.Trigger.Length + query.Text.Length)];
        await EnterAsync(new RichTextDocument(
            [new([new(before), new(new Mention(query.Trigger, item)), new(" ")])]).ToHtml());
    }

    /// <summary>Runs one of the app's slash commands, as typing this text into an empty editor and choosing the
    /// command with this name does. The text must end with a slash and the start of the command's name or of one
    /// of its keywords, such as <c>"Bring /pack"</c>. The command's <c>OnChosen</c> is called, and the editor's
    /// text becomes the text before the slash and then the command's HTML. Naming one of the editor's own commands,
    /// such as Heading 1, throws: only the editor's script changes a block, so give <see cref="EnterAsync"/> the HTML
    /// instead. A disabled or read-only editor throws.</summary>
    public async Task SlashCommandAsync(string typed, string command)
    {
        var editor = Unlocked();
        var query = Typing.MentionAt(typed, "/")
            ?? throw new InvalidOperationException($"\"{typed}\" doesn't end with a slash command being typed: a "
                + "slash at the start of a word, with no space after it.");
        if (!editor.HasAttribute("data-slash-commands"))
        {
            throw new InvalidOperationException("The editor has no slash commands: it needs SlashCommands, and its "
                + "script attaches once it renders interactively.");
        }

        var events = _scripts.RichTextMentions(editor);
        var list = $"[id='{editor.GetAttribute("aria-controls")}']";
        var number = int.Parse(_findAll(list)[0].GetAttribute("data-results-for")!, CultureInfo.InvariantCulture) + 1;
        await _scripts.InvokeAsync(() => events.OnQuery(query.Trigger, query.Text, number, "paragraph"));
        var options = _findAll($"{list} [role=option][data-part=option]").ToList();
        if (options.Count == 0)
        {
            throw new InvalidOperationException($"The editor lists no command for \"/{query.Text}\": it needs "
                + "SlashCommands, and a command with that text in its name or keywords.");
        }

        var option = Choices.Named(options, command);
        var id = option.GetAttribute("data-mention-id") ?? "";
        if (!id.StartsWith(RichTextCommand.IdPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"\"{command}\" is one of the editor's own commands, which only its "
                + "script runs. Give EnterAsync the HTML it would make instead.");
        }

        await _scripts.InvokeAsync(() => events.OnChosen(options.IndexOf(option), number));
        var html = await _scripts.RichTextInsertAsync(() => events.OnCommand(id));
        var put = RichTextDocument.Parse(html, events.Schema ?? RichTextSchema.Default).Paragraphs;
        var before = typed[..^(query.Trigger.Length + query.Text.Length)];
        var line = new RichTextParagraph([new(before)]);
        // As the script puts the HTML in: text joins the line being typed; a heading, quote or list starts under
        // it, or in its place when nothing was typed; and a rule, image or table stands apart, with a paragraph
        // after it when it comes last.
        IEnumerable<RichTextParagraph> text = put switch
        {
            [{ Kind: RichTextParagraphKind.Paragraph } first, ..] =>
                [new([.. line.Runs, .. first.Runs]), .. put.Skip(1)],
            [var first, ..] when before.Length == 0 && HoldsText(first) => put,
            _ => [line, .. put],
        };
        await EnterAsync(new RichTextDocument(HoldsText(put[^1]) ? text : [.. text, new([])]).ToHtml());
    }

    private static bool HoldsText(RichTextParagraph paragraph) => paragraph.Kind is not (RichTextParagraphKind.Rule
        or RichTextParagraphKind.Image or RichTextParagraphKind.TableCell);
}

/// <summary>The <c>TbTour</c> on the page.</summary>
public sealed class TbTourDriver
{
    private readonly Func<IReadOnlyList<IElement>> _find;

    internal TbTourDriver(Func<IReadOnlyList<IElement>> find) => _find = find;

    /// <summary>Whether the tour shows.</summary>
    public bool IsOpen => _find().Count > 0;

    /// <summary>The shown step's position, from 0.</summary>
    public int Step => int.Parse(Tour().GetAttribute("data-step")!, CultureInfo.InvariantCulture);

    /// <summary>The shown step's title.</summary>
    public string Title => Tour().QuerySelector("[data-part=title]")!.TextContent.Trim();

    /// <summary>Goes to the next step, as pressing Next does, or ends the tour as completed on the last step, as
    /// pressing Done does.</summary>
    public Task NextAsync() => PressAsync("next");

    /// <summary>Goes back a step, as pressing Back does. The first step has no Back button, so this throws there.
    /// </summary>
    public Task BackAsync() => PressAsync("back");

    /// <summary>Ends the tour as skipped, as pressing Skip tour does.</summary>
    public Task SkipAsync() => PressAsync("skip");

    private async Task PressAsync(string part)
    {
        var button = Tour().QuerySelector($"[data-part={part}]")
            ?? throw new InvalidOperationException($"The tour's step has no {part} button.");
        await button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private IElement Tour() => _find() is [var only] ? only
        : throw new InvalidOperationException(_find().Count == 0 ? "No tour shows." : "More than one tour shows.");
}

/// <summary>A <c>TbMarkdownEditor</c>, found by its label.</summary>
public sealed class TbMarkdownEditorDriver : TbFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbMarkdownEditorDriver(Func<IElement> find) : base(find) => _find = find;

    /// <summary>The markdown the editor holds.</summary>
    public string? Text => _find().GetAttribute("value");

    /// <summary>The HTML the preview shows, or null while it isn't shown.</summary>
    public string? PreviewHtml => Editor().QuerySelector("[data-part=preview]:not([hidden])")?.InnerHtml;

    /// <summary>Replaces the editor's markdown with this text, as typing it does, and leaves the editor. A read-only
    /// or disabled editor throws, since a user can't type into it.</summary>
    public async Task EnterAsync(string text)
    {
        Typing.ThrowIfLocked(_find(), "text");
        await _find().InputAsync(new ChangeEventArgs { Value = text });
        await LeaveAsync();
    }

    /// <summary>Shows the preview with the Preview button, unless it shows already. An editor whose preview isn't
    /// <c>MarkdownPreview.Toggle</c> throws, since it has no such button.</summary>
    public Task ShowPreviewAsync() => PreviewAsync(true);

    /// <summary>Hides the preview with the Preview button, showing the text again, unless it's hidden already. It
    /// throws as <see cref="ShowPreviewAsync"/> does.</summary>
    public Task HidePreviewAsync() => PreviewAsync(false);

    private async Task PreviewAsync(bool shown)
    {
        var button = Editor().QuerySelector("[data-part=preview-toggle]")
            ?? throw new InvalidOperationException("This editor has no Preview button; its Preview is Beside or None.");
        if ((button.GetAttribute("aria-pressed") == "true") != shown)
        {
            await button.ClickAsync(new MouseEventArgs { Detail = 1 });
        }
    }

    private IElement Editor() => _find().Closest("[data-part=editor]")!;
}

/// <summary>A <c>TbTreeView</c>, found by its accessible name, with the same helper names as the Playwright locator.
/// Rows are found by their path of texts from a root, such as <c>("Docs", "Guide")</c>, or from a branch's row.
/// </summary>
public sealed class TbTreeDriver
{
    private readonly Func<IElement> _find;
    private readonly string[] _branch;

    internal TbTreeDriver(Func<IElement> find) : this(find, []) { }

    private TbTreeDriver(Func<IElement> find, string[] branch) => (_find, _branch) = (find, branch);

    /// <summary>The texts of the rows that show, in order. In a branch, only the rows inside it count.</summary>
    public IReadOnlyList<string> Texts => [.. Shown().Select(row => row.GetAttribute("data-text") ?? "")];

    /// <summary>The rows inside the row at this path, as a driver whose paths start there, such as
    /// <c>Branch("Docs").SelectAsync("Guide")</c>. Its steps open the branch's row as they open the rows above.
    /// </summary>
    public TbTreeDriver Branch(params string[] path) => new(_find, [.. _branch, .. path]);

    /// <summary>The row with this text at any depth, which must be showing. It throws when no row or several rows show
    /// the text; <see cref="Item"/> tells them apart by their path.</summary>
    public IElement Row(string text)
    {
        List<IElement> rows = [.. Shown().Where(row => row.GetAttribute("data-text") == text)];
        return rows.Count == 1 ? rows[0] : throw new InvalidOperationException(rows.Count == 0
            ? $"No row shows {text}." : $"{rows.Count} rows show {text}, so find it by its path with Item.");
    }

    /// <summary>The row at this path, which must be showing. When several rows match, it is the first.</summary>
    public IElement Item(params string[] path) => At([.. _branch, .. path]);

    /// <summary>Every row at this path, for rows that share their text. Their parents must be open.</summary>
    public IReadOnlyList<IElement> Items(params string[] path) => Find([.. _branch, .. path]);

    private IElement At(string[] path) => Find(path).FirstOrDefault()
        ?? throw new InvalidOperationException($"No row shows at {string.Join(" > ", path)}.");

    private List<IElement> Find(string[] path)
    {
        var rows = OwnRows();
        List<IElement> found = [];
        Walk(0, 0);
        return found;

        // Each name is looked for after its parent row and before the next row at the parent's level.
        void Walk(int start, int depth)
        {
            for (var index = start; index < rows.Count; index++)
            {
                var level = int.Parse(rows[index].GetAttribute("aria-level")!, CultureInfo.InvariantCulture);
                if (level <= depth)
                {
                    break;
                }

                if (level == depth + 1 && rows[index].GetAttribute("data-text") == path[depth])
                {
                    if (depth == path.Length - 1)
                    {
                        found.Add(rows[index]);
                    }
                    else
                    {
                        Walk(index + 1, depth + 1);
                    }
                }
            }
        }
    }

    /// <summary>Opens each row along the path, as clicking its toggle does.</summary>
    public async Task ExpandAsync(params string[] path)
    {
        string[] full = [.. _branch, .. path];
        for (var depth = 1; depth <= full.Length; depth++)
        {
            var row = At(full[..depth]);
            if (row.GetAttribute("aria-expanded") == "false")
            {
                await row.QuerySelector("[data-part=toggle]")!.ClickAsync(new MouseEventArgs { Detail = 1 });
            }
        }
    }

    /// <summary>Closes the row at this path, as clicking its toggle does. It must be showing.</summary>
    public async Task CollapseAsync(params string[] path)
    {
        var row = Item(path);
        if (row.GetAttribute("aria-expanded") == "true")
        {
            await row.QuerySelector("[data-part=toggle]")!.ClickAsync(new MouseEventArgs { Detail = 1 });
        }
    }

    /// <summary>Opens the rows above the one at this path and activates it, as double-clicking it does. That raises
    /// the tree's <c>OnActivate</c>. It also opens or closes a row with children, unless the row is disabled, or can
    /// be selected and the tree has an <c>OnActivate</c>.</summary>
    public async Task ActivateAsync(params string[] path)
    {
        await ExpandAsync(path[..^1]);
        // A double-click starts with two clicks, which can select the row or open one that can't be selected.
        await Item(path).ClickAsync(new MouseEventArgs { Detail = 1 });
        await Item(path).ClickAsync(new MouseEventArgs { Detail = 2 });
        await Item(path).DoubleClickAsync(new MouseEventArgs { Detail = 2 });
    }

    /// <summary>Opens every row, and the rows that appear inside them as they open. In a branch, it opens the branch's
    /// row and the rows inside it. A tree inside a row is a tree of its own, which this leaves closed; open it with its
    /// own helper.</summary>
    public async Task ExpandAllAsync()
    {
        await ExpandAsync();
        while (Shown().FirstOrDefault(row => row.GetAttribute("aria-expanded") == "false") is { } row)
        {
            var key = row.GetAttribute("data-key");
            await row.QuerySelector("[data-part=toggle]")!.ClickAsync(new MouseEventArgs { Detail = 1 });
            // A disabled tree doesn't open, so the same row would be clicked forever.
            if (Shown().Any(again => again.GetAttribute("data-key") == key
                && again.GetAttribute("aria-expanded") == "false"))
            {
                throw new InvalidOperationException($"The row {row.GetAttribute("data-text")} didn't open.");
            }
        }
    }

    /// <summary>Opens the rows above the one at this path and selects it, as clicking it does.</summary>
    public async Task SelectAsync(params string[] path)
    {
        await ExpandAsync(path[..^1]);
        await Item(path).ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Opens the rows above the one at this path and checks it, as clicking its box does.</summary>
    public async Task CheckAsync(params string[] path)
    {
        await ExpandAsync(path[..^1]);
        await (Item(path).QuerySelector("[data-part=check]")
            ?? throw new InvalidOperationException($"The row at {string.Join(" > ", path)} has no box to check."))
            .ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Drops the row at <paramref name="row"/> where dragging it onto the top edge of the row at
    /// <paramref name="target"/> does: just before it, or where the tree's <c>MovePositions</c> sends that edge. Both
    /// rows must be showing, and the row must be one the tree lets move.</summary>
    public async Task DragBeforeAsync(string[] row, string[] target) =>
        await DragAsync(row, target, TreeMovePosition.Before);

    /// <summary>Drops the row at <paramref name="row"/> where dragging it onto the bottom edge of the row at
    /// <paramref name="target"/> does: just after it, which for an open row means above its first child, or where the
    /// tree's <c>MovePositions</c> sends that edge. Both rows must be showing, and the row must be one the tree lets
    /// move.</summary>
    public async Task DragAfterAsync(string[] row, string[] target) =>
        await DragAsync(row, target, TreeMovePosition.After);

    /// <summary>Drops the row at <paramref name="row"/> where dragging it onto the middle of the row at
    /// <paramref name="target"/> does: inside it, or where the tree's <c>MovePositions</c> sends the middle. Both rows
    /// must be showing, and the row must be one the tree lets move.</summary>
    public async Task DragIntoAsync(string[] row, string[] target) =>
        await DragAsync(row, target, TreeMovePosition.Inside);

    // Follows the tree's script: a selected row brings the other selected rows that can move, where a drop on a row's
    // top edge, middle or bottom edge goes depends on the places the tree allows, a drop below an open row goes above
    // its first child (or into the row when Before isn't allowed), and a drop on a moving row does nothing. The tree
    // then checks it with CanMove.
    private async Task DragAsync(string[] row, string[] target, TreeMovePosition gesture)
    {
        var source = Item(row);
        if (!source.HasAttribute("data-draggable"))
        {
            throw new InvalidOperationException($"The row at {string.Join(" > ", row)} can't be dragged: the tree "
                + "needs an OnMove and mustn't be disabled, and the row must be enabled, have a value and be allowed "
                + "by ItemMovable.");
        }

        var rows = OwnRows();
        List<IElement> moved = Selected(source)
            ? [.. rows.Where(other => Selected(other) && other.HasAttribute("data-draggable"))] : [source];
        var onto = Item(target);
        if (moved.Contains(onto))
        {
            return;
        }

        var places = (_find().GetAttribute("data-move-positions") ?? "").Split(' ');
        var position = PositionAt(gesture switch
        {
            TreeMovePosition.Before => 0.1,
            TreeMovePosition.After => 0.9,
            _ => 0.5,
        }, places);
        var next = rows.ElementAtOrDefault(rows.IndexOf(onto) + 1);
        if (position == TreeMovePosition.After && onto.GetAttribute("aria-expanded") == "true" && next is not null
            && Level(next) > Level(onto))
        {
            if (places.Contains("before"))
            {
                (onto, position) = (next, TreeMovePosition.Before);
            }
            else if (places.Contains("inside"))
            {
                position = TreeMovePosition.Inside;
            }
            else
            {
                return;
            }
        }

        if (!moved.Contains(onto))
        {
            await _find().TriggerEventAsync("ontbtreedrop", new TreeDropEventArgs
            {
                Rows = [.. moved.Select(other => other.GetAttribute("data-key")!)],
                Target = onto.GetAttribute("data-key")!,
                Position = position,
            });
        }

        static bool Selected(IElement other) => other.GetAttribute("aria-selected") == "true";

        // As the script splits a row among the allowed places.
        static TreeMovePosition PositionAt(double at, string[] places)
        {
            var edge = places.Contains("inside") ? 0.25 : 0.5;
            if (places.Contains("before") && at < edge)
            {
                return TreeMovePosition.Before;
            }

            if (places.Contains("after") && at >= 1 - edge)
            {
                return TreeMovePosition.After;
            }

            return places.Contains("inside") ? TreeMovePosition.Inside
                : places.Contains("before") ? TreeMovePosition.Before : TreeMovePosition.After;
        }
    }

    private static int Level(IElement row) => int.Parse(row.GetAttribute("aria-level")!, CultureInfo.InvariantCulture);

    // This tree's rows, or in a branch the rows after its row that sit deeper than it.
    private List<IElement> Shown()
    {
        var rows = OwnRows();
        if (_branch.Length == 0)
        {
            return rows;
        }

        var branch = At(_branch);
        return [.. rows.SkipWhile(row => row != branch).Skip(1).TakeWhile(row => Level(row) > Level(branch))];
    }

    // Leaves out the rows of a tree nested inside one of this tree's rows.
    private List<IElement> OwnRows()
    {
        var tree = _find();
        return [.. tree.QuerySelectorAll("[role=treeitem][data-key]").Where(row => row.Closest("[role=tree]") == tree)];
    }
}

/// <summary>A <c>TbDataGrid</c>, found by its table's accessible name.</summary>
public sealed class TbGridDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbGridDriver(Func<string, IReadOnlyList<IElement>> findAll, Func<IElement> find, ScriptCalls scripts) =>
        (_findAll, _find, _scripts) = (findAll, find, scripts);

    /// <summary>The rows the grid shows on its current page, each as its data cells' texts, in the order of
    /// <see cref="Columns"/>.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows => Texts(OwnRows);

    /// <summary>How many rows the grid shows on its current page.</summary>
    public int RowCount => _find().QuerySelectorAll(OwnRows).Length;

    /// <summary>The selected rows the grid shows on its current page, each as its data cells' texts, in the order of
    /// <see cref="Columns"/>.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Selected => Texts($"{OwnRows}[data-selected]");

    /// <summary>Whether the grid is loading rows, as it marks with <c>aria-busy</c>.</summary>
    public bool IsBusy => _find().GetAttribute("aria-busy") == "true";

    /// <summary>Waits until the grid has loaded its rows, such as from a <c>DataProvider</c> that answers later.
    /// </summary>
    public async Task WaitForRowsAsync()
    {
        // Read once first, so a grid that isn't there fails with its own message rather than inside bUnit's wait.
        if (IsBusy)
        {
            await _scripts.WaitForAsync(() => !IsBusy);
        }
    }

    // A grid nested in a detail row has rows of its own, which aren't this grid's.
    private const string OwnRows = ":scope > tbody > tr[data-part=row]";

    private IReadOnlyList<IReadOnlyList<string>> Texts(string rows) => [.. _find().QuerySelectorAll(rows)
        .Select(row => (IReadOnlyList<string>)[.. row.Children.Where(cell => cell.HasAttribute("data-column"))
            .Select(cell => cell.TextContent.Trim())])];

    /// <summary>The row with a cell whose text is exactly this.</summary>
    public IElement Row(string cell)
    {
        var rows = _find().QuerySelectorAll("tbody tr[data-part=row]")
            .Where(row => row.Children.Any(td => td.TextContent.Trim() == cell)).ToList();
        return rows.Count == 1 ? rows[0]
            : throw new InvalidOperationException(rows.Count == 0
                ? $"No row has a cell that reads \"{cell}\"."
                : $"{rows.Count} rows have a cell that reads \"{cell}\".");
    }

    /// <summary>The cell under the column with this title in the row with a cell whose text is exactly
    /// <paramref name="row"/>.</summary>
    public IElement Cell(string row, string column) =>
        Row(row).QuerySelector($"td[data-column=\"{Header(column).GetAttribute("data-column")}\"]")
        ?? throw new InvalidOperationException($"The row \"{row}\" has no cell under \"{column}\".");

    /// <summary>Selects the cells from the one under <paramref name="fromColumn"/> in the row <paramref name="fromRow"/>
    /// to the one under <paramref name="toColumn"/> in the row <paramref name="toRow"/>, as a drag across them does. Each
    /// row is found by a cell whose text is exactly that value. Throws when the grid's <c>CellSelection</c> is off.
    /// </summary>
    public async Task SelectCellsAsync(string fromRow, string fromColumn, string toRow, string toColumn)
    {
        var table = _find();
        if (!table.HasAttribute("data-cell-selection"))
        {
            throw new InvalidOperationException("The grid doesn't select cells; set its CellSelection.");
        }

        var (from, to) = (Cell(fromRow, fromColumn), Cell(toRow, toColumn));
        await table.TriggerEventAsync("ontbcellrange", new CellRangeEventArgs
        {
            FromRow = from.ParentElement!.GetAttribute("data-key"),
            FromColumn = from.GetAttribute("data-column"),
            ToRow = to.ParentElement!.GetAttribute("data-key"),
            ToColumn = to.GetAttribute("data-column"),
        });
    }

    /// <summary>The text of the selected range's cells, row by row, as Ctrl+C copies them; empty without a range.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> SelectedCells =>
        _find().Closest("[data-part=grid]")?.QuerySelector(":scope > [data-part=copy]") is { } copy
            ? [.. copy.QuerySelectorAll("tr").Select(row => (IReadOnlyList<string>)
                [.. row.QuerySelectorAll("td").Select(cell => cell.TextContent)])]
            : [];

    /// <summary>Sorts the rows by this column alone, ascending or descending, clicking its header until it shows that
    /// order.</summary>
    public async Task SortAsync(string column, bool descending = false)
    {
        var wanted = descending ? "descending" : "ascending";
        // A plain click sorts by the column alone; after that it reverses the order, then stops sorting by the column.
        for (var clicks = 0; clicks < 4; clicks++)
        {
            var header = Header(column);
            if (header.GetAttribute("aria-sort") == wanted && _find().QuerySelector("[data-part=sort-order]") is null)
            {
                return;
            }

            await (header.QuerySelector("[data-part=sort]") ?? throw new InvalidOperationException(
                $"The \"{column}\" column can't be sorted; make it Sortable.")).ClickAsync(new MouseEventArgs());
        }

        throw new InvalidOperationException($"The \"{column}\" column didn't sort {wanted}.");
    }

    /// <summary>Goes to this page with the grid's pager, as <see cref="TbPaginationDriver.GoToPageAsync"/> does.
    /// </summary>
    public async Task GoToPageAsync(int number)
    {
        var pager = new TbPaginationDriver(() =>
            _find().Closest("[data-part=grid]")?.QuerySelector(":scope > [data-part=pager] nav")
            ?? throw new InvalidOperationException(
                "The grid shows no page buttons; it needs a PageSize and more than one page of rows."));
        await pager.GoToPageAsync(number);
    }

    /// <summary>Clicks the row with a cell whose text is exactly this, as the browser reports a click on a plain cell
    /// away from the row's controls: the grid raises <c>OnRowClick</c> and, with <c>RowClickSelects</c>, selects it.
    /// </summary>
    public async Task ClickRowAsync(string cell) =>
        await Row(cell).TriggerEventAsync("ontbrowclick", new RowClickEventArgs());

    /// <summary>Clicks the Save changes button of a grid whose <c>EditMode</c> is <c>Batch</c>. Throws when the grid
    /// holds no changes or doesn't edit in batches; an edit still open doesn't count as a change until it ends.
    /// </summary>
    public async Task SaveChangesAsync() =>
        await BatchButton("save-changes").ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Clicks the Discard changes button of a grid whose <c>EditMode</c> is <c>Batch</c>. Throws when the grid
    /// holds no changes or doesn't edit in batches; an edit still open doesn't count as a change until it ends.
    /// </summary>
    public async Task DiscardChangesAsync() =>
        await BatchButton("discard-changes").ClickAsync(new MouseEventArgs { Detail = 1 });

    private IElement BatchButton(string part)
    {
        var button = _find().Closest("[data-part=grid]")?.QuerySelector($":scope > [data-part=batch-bar] [data-part={part}]")
            ?? throw new InvalidOperationException("The grid has no batch bar; set its EditMode to Batch.");
        return button.GetAttribute("aria-disabled") == "true"
            ? throw new InvalidOperationException("The grid holds no changes.") : button;
    }

    /// <summary>Opens the tree grid row with a cell whose text is exactly this, with its button, so its child rows
    /// show. A row already open stays open.</summary>
    public async Task ExpandAsync(string row) => await SetExpandedAsync(row, true);

    /// <summary>Closes the tree grid row with a cell whose text is exactly this, with its button, so its child rows
    /// hide. A row already closed stays closed.</summary>
    public async Task CollapseAsync(string row) => await SetExpandedAsync(row, false);

    private Task SetExpandedAsync(string row, bool expanded)
    {
        var found = Row(row);
        var toggle = found.QuerySelector("[data-part=tree-toggle]") ?? throw new InvalidOperationException(
            $"The row \"{row}\" has no child rows to show; a tree grid's row needs children from its ChildrenSelector.");
        return found.GetAttribute("aria-expanded") == (expanded ? "true" : "false") ? Task.CompletedTask
            : toggle.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Moves the row with a cell whose text is exactly <paramref name="row"/> to <paramref name="index"/>
    /// among all the rows, on every page, through its handle's menu as a user would: Move to start or Move to end for
    /// the first or last place, else Move up or Move down a place at a time, so <c>CanMove</c> and
    /// <c>OnMove</c> see each step. A step the app refuses ends the move. The row must be on the page shown, and the
    /// grid shows the page it lands on. Throws when the rows can't move or <paramref name="index"/> is past the last
    /// row.</summary>
    public async Task MoveRowAsync(string row, int index) => await HandleMenu.MoveAsync(
        () => MovableRow(row).Children.First(cell => cell.ClassList.Contains("tb-data-grid__handle"))
            .FirstElementChild!,
        () => int.Parse(MovableRow(row).GetAttribute("data-index")!, CultureInfo.InvariantCulture),
        int.Parse(_find().GetAttribute("data-count") ?? "0", CultureInfo.InvariantCulture), index, row);

    private IElement MovableRow(string cell) => Row(cell) is var row && row.HasAttribute("data-index") ? row
        : throw new InvalidOperationException("The grid's rows can't move: it needs an OnMove and in-memory Items, "
            + "and mustn't be sorted, filtered or grouped.");

    /// <summary>Drags the row with a cell whose text is exactly <paramref name="row"/> by its handle onto the top half
    /// of the row with a cell whose text is exactly <paramref name="target"/>, to move it just before that row,
    /// reporting the drag and the drop as the grid's script would. Both rows must be on the page shown, and the grid
    /// needs a name no other grid or sortable list shares, by which the drag reaches its script.</summary>
    public async Task DragRowBeforeAsync(string row, string target) => await DragRowAsync(row, target, before: true);

    /// <summary>Drags the row with a cell whose text is exactly <paramref name="row"/> by its handle onto the bottom
    /// half of the row with a cell whose text is exactly <paramref name="target"/>, to move it just after that row,
    /// reporting the drag and the drop as the grid's script would. Both rows must be on the page shown.</summary>
    public async Task DragRowAfterAsync(string row, string target) => await DragRowAsync(row, target, before: false);

    private Task DragRowAsync(string row, string target, bool before)
    {
        var (source, onto) = (MovableRow(row), MovableRow(target));
        HandleMenu.TriggerOf(source.Children.First(cell => cell.ClassList.Contains("tb-data-grid__handle"))
            .FirstElementChild!, row);
        return Drag.DropAsync(_scripts, _find().GetAttribute("aria-label"), source, onto, before);
    }


    /// <summary>Selects the row with a cell whose text is exactly this, with its checkbox or radio button, unless it
    /// is selected already.</summary>
    public async Task SelectAsync(string row)
    {
        var found = Row(row);
        var box = found.QuerySelector("[data-part=select] input")
            ?? throw new InvalidOperationException("The grid's rows can't be selected; set its SelectionMode.");
        if (!found.HasAttribute("data-selected"))
        {
            await box.ChangeAsync(new ChangeEventArgs());
        }
    }

    /// <summary>Filters the column with this title by this text. In the filter row, it types the text and commits it,
    /// as pressing Enter does, so a text filter applies without waiting for typing to pause; in a filter menu, it types
    /// the text as the value and applies the menu. For a text or number filter; choose a row's choice filter with
    /// <c>cut.Tb().Select("Filter Status")</c>, or a list filter's choices with <see cref="FilterChoicesAsync"/>.
    /// </summary>
    public async Task FilterAsync(string column, string text)
    {
        if (Menu(column) is not null)
        {
            await EnterAsync(ValueInput(column) ?? throw new InvalidOperationException(
                $"The \"{column}\" column's filter takes no typed value."), text);
            await ApplyAsync(column);
            return;
        }

        var input = FilterCell(column).QuerySelector("input")
            ?? throw new InvalidOperationException($"The \"{column}\" column has no filter; make it Filterable.");
        if (input.Matches("input[type=number], input[role=spinbutton]"))
        {
            await new TbNumberFieldDriver(() => input).EnterAsync(text);
            return;
        }

        // Send the text as an input and then as a change, the way typing and then pressing Enter would.
        await input.InputAsync(new ChangeEventArgs { Value = text });
        await input.ChangeAsync(new ChangeEventArgs { Value = text });
    }

    /// <summary>In the filter menu of the column with this title, chooses the condition with this name, such as
    /// "At least", types the text as the value, unless the condition takes none, and applies the menu.</summary>
    public async Task FilterAsync(string column, string condition, string text)
    {
        var menu = Menu(column) ?? throw new InvalidOperationException(
            $"The \"{column}\" column has no filter menu; set the grid's FilterMode to Menu.");
        if (menu.QuerySelector("[role=combobox]") is null)
        {
            throw new InvalidOperationException($"The \"{column}\" column's filter has no conditions; use "
                + "FilterAsync(column, text) or FilterChoicesAsync.");
        }

        // Found again on each use, since opening the list renders new markup.
        await new TbSelectDriver(() => Menu(column)!.QuerySelector("[role=combobox]")!, _findAll, _scripts)
            .ChooseAsync(condition);
        // Choosing renders the menu again, without a value field for a condition that takes none.
        if (ValueInput(column) is { } input)
        {
            await EnterAsync(input, text);
        }

        await ApplyAsync(column);
    }

    /// <summary>Leaves exactly these choices picked in the list filter of the column with this title, naming each as
    /// the filter shows it; pass none to clear the filter. Works with the filter row's multiple select and with a
    /// filter menu's checkboxes, which it then applies.</summary>
    public async Task FilterChoicesAsync(string column, params string[] choices)
    {
        if (Menu(column) is null)
        {
            var cell = FilterCell(column);
            if (!IsList(cell))
            {
                throw NotAList(column);
            }

            if (cell.QuerySelector("[data-part=clear]") is { } clear)
            {
                await clear.ClickAsync(new MouseEventArgs { Detail = 1 });
            }

            if (choices.Length > 0)
            {
                await new TbSelectDriver(() => FilterCell(column).QuerySelector("[role=combobox]")!, _findAll, _scripts)
                    .ChooseAsync(choices);
            }

            return;
        }

        var names = Menu(column)!.QuerySelectorAll("input[type=checkbox]")
            .Select(box => Choices.NameOf(box.Closest("label")!)).ToList();
        if (names.Count == 0)
        {
            throw new InvalidOperationException(
                $"The \"{column}\" column's filter isn't a list; give the column FilterChoices.");
        }

        if (choices.FirstOrDefault(choice => !names.Contains(choice)) is { } missing)
        {
            throw new InvalidOperationException($"The \"{column}\" column's filter has no choice \"{missing}\".");
        }

        foreach (var name in names)
        {
            // Each change renders the menu again, so the box is found afresh.
            var box = Menu(column)!.QuerySelectorAll("input[type=checkbox]")
                .Single(other => Choices.NameOf(other.Closest("label")!) == name);
            if (box.HasAttribute("checked") != choices.Contains(name))
            {
                await box.ChangeAsync(new ChangeEventArgs { Value = choices.Contains(name) });
            }
        }

        await ApplyAsync(column);
    }

    private const string ValueSelector = "[data-part=input]:not([role=combobox]):not([type=checkbox])";

    /// <summary>The columns' titles, in the order they show.</summary>
    public IReadOnlyList<string> Columns => _find().QuerySelectorAll("thead th[data-title]")
        .Select(header => header.GetAttribute("data-title")!).ToList();

    /// <summary>The text of the totals row's cell under the column with this title, such as <c>"Sum 42.1"</c>, or
    /// null when the column shows no total.</summary>
    public string? Total(string column)
    {
        var key = Header(column).GetAttribute("data-column");
        return TotalText(_find().QuerySelector($"tfoot [data-part=totals] td[data-column=\"{key}\"]"));
    }

    /// <summary>The text of each open group's totals cell under the column with this title, in the order the groups
    /// show, with null for a group that shows no total there. The grid shows these rows with <c>GroupTotals</c>.
    /// </summary>
    public IReadOnlyList<string?> GroupTotals(string column)
    {
        var key = Header(column).GetAttribute("data-column");
        return [.. _find().QuerySelectorAll($"tbody [data-part=group-totals] td[data-column=\"{key}\"]")
            .Select(TotalText)];
    }

    // The row's name for screen readers isn't part of the total.
    private static string? TotalText(IElement? cell)
    {
        var shown = (IElement?)cell?.Clone();
        shown?.QuerySelector("[data-part=totals-name]")?.Remove();
        var text = shown?.TextContent;
        return string.IsNullOrWhiteSpace(text) ? null
            : string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Resizes the column with this title to this many pixels, as the grid's script reports a drag of its
    /// header's edge. bUnit has no layout, so the other columns keep only the widths the grid already gave them, and
    /// the column's <c>MinWidth</c>, which the browser measures, doesn't apply.</summary>
    public async Task ResizeColumnAsync(string column, double width)
    {
        var header = Header(column);
        if (header.QuerySelector("[data-part=resize]") is null)
        {
            throw new InvalidOperationException($"The \"{column}\" column can't be resized; set the grid's "
                + "ResizableColumns and leave the column's Resizable on.");
        }

        var widths = _find().QuerySelectorAll("thead th[data-width]").ToDictionary(
            other => other.GetAttribute("data-column")!,
            other => double.Parse(other.GetAttribute("data-width")!, CultureInfo.InvariantCulture));
        widths[header.GetAttribute("data-column")!] = width;
        await _find().TriggerEventAsync("ontbcolumnwidths", new ColumnWidthsEventArgs { Widths = widths });
    }

    private IElement Header(string column) => _find().QuerySelectorAll("thead th")
        .FirstOrDefault(header => header.GetAttribute("data-title") == column)
        ?? throw new InvalidOperationException($"No column is titled \"{column}\".");

    private IElement? Menu(string column) => Header(column).QuerySelector("[data-part=filter-menu]");

    private IElement FilterCell(string column) =>
        _find().QuerySelector($"[data-part=filter][data-column=\"{Header(column).GetAttribute("data-column")}\"]")
        ?? throw new InvalidOperationException($"The \"{column}\" column has no filter; make it Filterable.");

    private IElement? ValueInput(string column) => Menu(column)!.QuerySelector(ValueSelector);

    // A row's list filter is a multiple select; an enum's or a bool's is a select of one.
    private static bool IsList(IElement cell) =>
        cell.QuerySelector("[role=combobox]")?.GetAttribute("aria-controls") is { } list
        && cell.QuerySelector($"[id='{list}'][aria-multiselectable=true]") is not null;

    private static InvalidOperationException NotAList(string column) => new($"The \"{column}\" column's filter "
        + "isn't a list; give the column FilterChoices, or set the grid's FilterMode to Menu for an enum or bool.");

    private Task ApplyAsync(string column) => (Menu(column)!.QuerySelector("[data-part=filter-apply]")
        ?? throw new InvalidOperationException(
            $"The \"{column}\" column's filter is a template; use the controls it draws."))
        .ClickAsync(new MouseEventArgs { Detail = 1 });

    // A text field that updates as the user types takes input; the rest commit on change.
    private static Task EnterAsync(IElement input, string text) =>
        input.Matches("input[type=number], input[role=spinbutton]")
            ? new TbNumberFieldDriver(() => input).EnterAsync(text)
            : input.HasAttribute("blazor:oninput") ? input.InputAsync(new ChangeEventArgs { Value = text })
            : input.ChangeAsync(new ChangeEventArgs { Value = text });
}

/// <summary>A <c>TbCommandPalette</c>, found by its label.</summary>
public sealed class TbCommandPaletteDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbCommandPaletteDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_find, _findAll) = (find, findAll);

    /// <summary>Whether the palette is open.</summary>
    public bool IsOpen => SearchBox() is not null;

    /// <summary>Opens the palette as its shortcut, such as Ctrl+K, does. For an open palette it does nothing, since
    /// pressing the shortcut again would close it. Throws for a palette with no shortcut; click the app's own button
    /// instead.</summary>
    public async Task OpenAsync()
    {
        var marker = _find();
        if (!marker.HasAttribute("data-shortcut"))
        {
            throw new InvalidOperationException(
                "This command palette has no shortcut; open it with the app's own button.");
        }

        if (!IsOpen)
        {
            await marker.QuerySelector("[data-part=hotkey]")!.TriggerEventAsync("ontbhotkey", EventArgs.Empty);
        }
    }

    /// <summary>The names of the commands it shows, in order.</summary>
    public IReadOnlyList<string> Results => [.. Listbox().QuerySelectorAll("[data-part=option]").Select(NameOf)];

    /// <summary>Types this text in the search box, as the user does, so the list shows what matches.</summary>
    public async Task SearchAsync(string text) => await Open().InputAsync(new ChangeEventArgs { Value = text });

    /// <summary>Runs the command with this name, as clicking it does, and returns once the app's <c>OnChoose</c> has
    /// run. Throws for a closed palette, or a command it doesn't show or that is disabled.</summary>
    public async Task ChooseAsync(string name)
    {
        var option = Listbox().QuerySelectorAll("[data-part=option]")
            .SingleOrDefault(option => NameOf(option) == name)
            ?? throw new InvalidOperationException($"The command palette shows no command named \"{name}\".");
        if (option.GetAttribute("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"The command \"{name}\" is disabled.");
        }

        await option.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    // A component inside a template can have a label part of its own, so only the palette's own label counts.
    private static string NameOf(IElement option) => (option.Children
        .FirstOrDefault(child => child.ClassList.Contains("tb-command-palette__text"))?.Children
        .FirstOrDefault(child => child.GetAttribute("data-part") == "label") ?? option).TextContent.Trim();

    private IElement? SearchBox()
    {
        var label = _find().GetAttribute("data-command-palette");
        return _findAll(".tb-command-palette__input")
            .SingleOrDefault(input => input.GetAttribute("aria-label") == label);
    }

    private IElement Open() => SearchBox() ?? throw new InvalidOperationException(
        "The command palette is closed; open it as the app does, such as with its button.");

    private IElement Listbox() => _findAll($"[id='{Open().GetAttribute("aria-controls")}']").Single();
}

/// <summary>A <c>TbKanban</c>, found by its <c>AriaLabel</c>. Each column is a sortable list named by its title.
/// </summary>
public sealed class TbKanbanDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbKanbanDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts)
        => (_find, _findAll, _scripts) = (find, findAll, scripts);

    /// <summary>The columns' titles, left to right.</summary>
    public IReadOnlyList<string> Columns => [.. ColumnElements().Select(TitleOf)];

    /// <summary>The names of the cards in the column titled <paramref name="column"/>, top first.</summary>
    public IReadOnlyList<string> Cards(string column) => Column(column).Items;

    /// <summary>The column titled <paramref name="column"/>, as a sortable list: its cards move within it with
    /// <c>MoveAsync</c>, or to another column with <c>MoveToListAsync</c>. Throws when the board has no such column.
    /// </summary>
    public TbSortableDriver Column(string column) => new(() => ColumnElement(column)
        .QuerySelector("[data-part=list][data-sortable]")!, _findAll, _scripts);

    /// <summary>Moves the card named <paramref name="card"/> to the end of the column titled <paramref name="column"/>,
    /// through its handle's menu as a user would. Throws when the board has no such card or column.</summary>
    public async Task MoveToColumnAsync(string card, string column)
    {
        ColumnElement(column);
        var from = Columns.FirstOrDefault(title => Cards(title).Contains(card))
            ?? throw new InvalidOperationException($"This board has no card \"{card}\".");
        await Column(from).MoveToListAsync(card, column);
    }

    private List<IElement> ColumnElements() => [.. _find().QuerySelectorAll(":scope > [data-part=column]")];

    private static string TitleOf(IElement column) =>
        column.QuerySelector(":scope > [data-part=header] > [data-part=title]")!.TextContent.Trim();

    private IElement ColumnElement(string title) => ColumnElements().FirstOrDefault(column => TitleOf(column) == title)
        ?? throw new InvalidOperationException($"This board has no column \"{title}\".");
}

/// <summary>A <c>TbSortable</c>, found by its label. Its items are named by the list's <c>ItemLabel</c>, or by their
/// text when it has none. Its drags reach the list's script by that label, so they need one no other sortable list or
/// grid shares.</summary>
public sealed class TbSortableDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbSortableDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts)
        => (_find, _findAll, _scripts) = (find, findAll, scripts);

    /// <summary>The items' names, in the order they show.</summary>
    public IReadOnlyList<string> Items => [.. ItemElements().Select(NameOf)];

    /// <summary>Moves the item named <paramref name="item"/> to <paramref name="index"/>, through its handle's menu as
    /// a user would: Move to start or Move to end for the first or last place, else Move up or Move down a place at a
    /// time, so <c>CanMove</c> and <c>OnMove</c> see each step. A step the app refuses ends the move. Throws when the
    /// list is disabled or <paramref name="index"/> is past its last item.</summary>
    public async Task MoveAsync(string item, int index) => await HandleMenu.MoveAsync(
        () => Item(item).Children.First(child => child.GetAttribute("data-part") == "handle"),
        () => ItemElements().IndexOf(Item(item)), ItemElements().Count, index, item);

    /// <summary>Moves the item named <paramref name="item"/> to the end of the list labeled <paramref name="list"/> in
    /// its group, through its handle's menu as a user would. Throws when either list is disabled or no other list in
    /// the group has that label.</summary>
    public async Task MoveToListAsync(string item, string list)
    {
        var menu = Item(item).Children.First(child => child.GetAttribute("data-part") == "handle");
        var trigger = HandleMenu.TriggerOf(menu, item);
        var choice = menu.QuerySelectorAll("[role=menuitem][data-list]")
            .FirstOrDefault(choice => choice.GetAttribute("data-list") == list)
            ?? throw new InvalidOperationException($"\"{item}\" can't move to \"{list}\": no other list in its group "
                + "has that label.");
        await TbMenuDriver.ClickAsync(trigger, choice);
    }

    /// <summary>Drags the item named <paramref name="item"/> by its handle onto the top half of the item
    /// <paramref name="target"/>, to move it just before that item, reporting the drag and the drop as the list's
    /// script would.</summary>
    public async Task DragBeforeAsync(string item, string target) => await DragAsync(item, target, before: true);

    /// <summary>Drags the item named <paramref name="item"/> by its handle onto the bottom half of the item
    /// <paramref name="target"/>, to move it just after that item, reporting the drag and the drop as the list's
    /// script would.</summary>
    public async Task DragAfterAsync(string item, string target) => await DragAsync(item, target, before: false);

    /// <summary>Drags the item named <paramref name="item"/> by its handle into the list labeled
    /// <paramref name="list"/> in its group, onto the top half of the item <paramref name="before"/> to land just
    /// before it, or below that list's last item (onto the list itself when it's empty) when
    /// <paramref name="before"/> is null, reporting the drag and the drop as the lists' script would. Throws when
    /// either list is disabled, when no other list in its group has that label, or when the item
    /// <paramref name="before"/> isn't there.</summary>
    public async Task DragToListAsync(string item, string list, string? before = null)
    {
        var source = Item(item);
        Handle(source, item);
        var others = _findAll("[data-part=list][data-sortable]")
            .Where(other => other.GetAttribute("aria-label") == list).ToList();
        if (others is not [var other])
        {
            throw new InvalidOperationException($"Expected one sortable list labeled \"{list}\", but found "
                + $"{others.Count}.");
        }

        // Only another list of the item's group takes it.
        var group = _find().GetAttribute("data-group");
        if (other.GetAttribute("data-list-id") == _find().GetAttribute("data-list-id") || group is null
            || other.GetAttribute("data-group") != group)
        {
            throw new InvalidOperationException($"\"{item}\" can't move to \"{list}\", which isn't another list of its "
                + "group.");
        }

        if (other.ParentElement?.ClassList.Contains("tb-sortable--disabled") == true)
        {
            throw new InvalidOperationException($"\"{item}\" can't move to \"{list}\", because that list is disabled.");
        }

        var targets = new TbSortableDriver(() => other, _findAll, _scripts);
        var target = before is not null ? targets.Item(before) : targets.ItemElements().LastOrDefault();
        var (id, listId) = (source.GetAttribute("data-key")!, other.GetAttribute("data-list-id")!);
        var events = _scripts.Sortable(_find().GetAttribute("aria-label"));
        await _scripts.InvokeAsync(async () =>
        {
            await events.OnDragOverList(id, listId, target?.GetAttribute("data-key"), before is not null);
            await events.OnDropList(id, listId, target?.GetAttribute("data-key"), before is not null);
        });
    }

    private Task DragAsync(string item, string target, bool before)
    {
        var (source, onto) = (Item(item), Item(target));
        Handle(source, item);
        return Drag.DropAsync(_scripts, _find().GetAttribute("aria-label"), source, onto, before);
    }

    // Finds the item's handle, and throws when its list is disabled.
    private static void Handle(IElement item, string name) => HandleMenu.TriggerOf(
        item.Children.First(child => child.GetAttribute("data-part") == "handle"), name);

    private List<IElement> ItemElements() =>
        [.. _find().Children.Where(child => child.GetAttribute("data-part") == "item")];

    private static string NameOf(IElement item) => item.GetAttribute("data-text")
        ?? item.Children.First(child => child.ClassList.Contains("tb-sortable__content")).TextContent.Trim();

    private IElement Item(string name)
    {
        var items = ItemElements().Where(item => NameOf(item) == name).ToList();
        return items.Count == 1 ? items[0]
            : throw new InvalidOperationException($"Expected one item named \"{name}\", found {items.Count}.");
    }
}

// A drag by a handle within one list or grid: the script reports the item over the target, then drops it there.
internal static class Drag
{
    public static async Task DropAsync(ScriptCalls scripts, string? list, IElement item, IElement target, bool before)
    {
        var events = scripts.Sortable(list);
        var (id, targetId) = (item.GetAttribute("data-key")!, target.GetAttribute("data-key")!);
        await scripts.InvokeAsync(async () =>
        {
            await events.OnDragOver(id, targetId, before);
            await events.OnDrop(id, targetId, before);
        });
    }
}

// Moves an item or a row through its handle's menu, whose items are Move up, Move down, Move to start and Move to end
// in that order, so they're found by place, whatever the app's labels call them.
internal static class HandleMenu
{
    // Finds the handle's menu button, and throws when its list is disabled.
    internal static IElement TriggerOf(IElement menu, string name)
    {
        var trigger = menu.QuerySelector("[aria-haspopup=menu]")!;
        return trigger.HasAttribute("disabled") || trigger.GetAttribute("aria-disabled") == "true"
            ? throw new InvalidOperationException($"\"{name}\" can't move, because its list is disabled.")
            : trigger;
    }

    internal static async Task MoveAsync(Func<IElement> handle, Func<int> place, int count, int index, string name)
    {
        // A grid whose rows can't move has no count, so finding the row first throws the reason, not an out-of-range
        // index.
        var from = place();
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);
        while (from != index)
        {
            var menu = handle();
            var trigger = TriggerOf(menu, name);

            var step = index == 0 ? 2 : index == count - 1 ? 3 : index < from ? 0 : 1;
            await TbMenuDriver.ClickAsync(trigger, menu.QuerySelectorAll("[role=menuitem]")[step]);
            var now = place();
            if (now == from)
            {
                return;
            }

            from = now;
        }
    }
}

/// <summary>A <c>TbTabs</c>, found by its accessible name.</summary>
public sealed class TbTabsDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbTabsDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts) =>
        (_find, _findAll, _scripts) = (find, findAll, scripts);

    /// <summary>The helpers inside the panel of the tab with this text, such as
    /// <c>cut.Tb().Tabs().Panel("Route").TextField("Start")</c>, so a field that every panel has can be told apart.
    /// Throws when the tab's panel isn't in the markup, as happens for a panel not yet shown unless
    /// <c>RenderAllPanels</c> is set.</summary>
    public TrailblazorMarkup Panel(string tab) =>
        new TrailblazorMarkup(_findAll, _scripts).Within(() => PanelElement(tab));

    /// <summary>The panel of the tab with this text, to assert on what it shows. Throws when the panel isn't in the
    /// markup, as <see cref="Panel"/> does.</summary>
    public IElement PanelElement(string tab) =>
        Find(tab).GetAttribute("aria-controls") is { } panel ? _findAll($"[id='{panel}']").Single()
            : throw new InvalidOperationException($"The panel of the tab \"{tab}\" isn't in the markup; select the "
                + "tab first, or set RenderAllPanels.");

    /// <summary>The tabs' texts, in order.</summary>
    public IReadOnlyList<string> Tabs =>
        _find().QuerySelectorAll("[role=tab]").Select(tab => tab.TextContent.Trim()).ToList();

    /// <summary>The selected tab's text.</summary>
    public string? Selected => _find().QuerySelector("[role=tab][aria-selected=true]")?.TextContent.Trim();

    /// <summary>Selects the tab with this text, as clicking it does.</summary>
    public async Task SelectAsync(string tab) => await Find(tab).ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Closes the tab with this text, as clicking its close mark does. Throws for a tab that can't be closed.
    /// </summary>
    public async Task CloseAsync(string tab) =>
        await (Find(tab).QuerySelector("[data-part=close]")
            ?? throw new InvalidOperationException($"The tab \"{tab}\" can't be closed."))
        .ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Clicks the add button that follows the tab list. Throws for tabs with no add button.</summary>
    public async Task AddAsync()
    {
        var add = _find().ParentElement?.Children.FirstOrDefault(cell => cell.ClassName == "tb-tabs__actions")
            ?.QuerySelector("[data-part=add]");
        await (add ?? throw new InvalidOperationException("The tabs have no add button."))
            .ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private IElement Find(string tab) =>
        _find().QuerySelectorAll("[role=tab]").SingleOrDefault(element => element.TextContent.Trim() == tab)
            ?? throw new InvalidOperationException($"No tab reads \"{tab}\".");
}

/// <summary>A <c>TbHotkey</c>, found by its keys.</summary>
public sealed class TbHotkeyDriver
{
    private readonly Func<IElement> _find;

    internal TbHotkeyDriver(Func<IElement> find) => _find = find;

    /// <summary>Fires the hotkey as its script does when its keys are pressed where it applies. Throws for a disabled
    /// hotkey, whose keys reach the page instead.</summary>
    public async Task PressAsync()
    {
        var hotkey = _find();
        if (hotkey.HasAttribute("data-disabled"))
        {
            throw new InvalidOperationException($"The hotkey \"{hotkey.GetAttribute("data-keys")}\" is disabled.");
        }

        await hotkey.TriggerEventAsync("ontbhotkey", EventArgs.Empty);
    }
}

/// <summary>A <c>TbContextMenu</c>, found by its menu's name.</summary>
public sealed class TbContextMenuDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;
    private readonly string _key;

    internal TbContextMenuDriver(Func<IElement> find, ScriptCalls scripts, string key = "") =>
        (_find, _scripts, _key) = (find, scripts, key);

    /// <summary>The same context menu, opened on the element inside it with this <c>data-context-key</c>, such as a
    /// row's id: <c>cut.Tb().ContextMenu().On("ridge-loop").ChooseAsync("Delete")</c>.</summary>
    public TbContextMenuDriver On(string key) => new(_find, _scripts, key);

    /// <summary>Opens the menu as a right-click would and clicks the item with this name, through the submenus named
    /// before it, as in <c>ChooseAsync("Move to", "Archive")</c>. A link item also takes the test's
    /// <c>NavigationManager</c> to its address, unless it opens in a new tab. Throws for a disabled context menu or
    /// item, a key or name it doesn't have, or a static page, where the browser's own menu opens instead.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        var target = _find();
        if (target.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("This context menu is on a static page, where the browser's own menu "
                + "opens; render it on an interactive page.");
        }

        if (target.HasAttribute("data-disabled"))
        {
            throw new InvalidOperationException("This context menu is disabled.");
        }

        if (_key.Length > 0 && !target.QuerySelectorAll("[data-context-key]")
            .Any(element => element.GetAttribute("data-context-key") == _key))
        {
            throw new InvalidOperationException($"The context menu has no element with the key \"{_key}\".");
        }

        // The menu's script asks for another key's items the same way before it shows them.
        if (Menu(target).GetAttribute("data-key") != _key)
        {
            await target.TriggerEventAsync("ontbcontextmenuopen", new ContextMenuOpenEventArgs { Key = _key });
            target = _find();
        }

        var list = Menu(target);
        foreach (var name in path[..^1])
        {
            var opener = TbMenuDriver.Find(list, name, "[data-part=submenu-trigger]", "submenu");
            list = target.QuerySelector($"[id='{opener.GetAttribute("aria-controls")}']")!;
        }

        var item = TbMenuDriver.Find(list, path[^1], TbMenuDriver.Items, "item");
        await TbMenuDriver.ClickAsync(target, item);
        _scripts.Follow(item);
    }

    internal static IElement Menu(IElement target) =>
        target.Children.Single(child => child.GetAttribute("role") == "menu");
}

/// <summary>A <c>TbViewport</c>, found by its box's <c>Label</c>. Its controls are clicked as a pointer would; the
/// keys, the wheel and a drag need a browser.</summary>
public sealed class TbViewportDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbViewportDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_find, _findAll) = (find, findAll);

    /// <summary>The zoom, where 1 shows the content at its own size.</summary>
    public double Zoom => double.Parse(_find().GetAttribute("data-zoom")!, CultureInfo.InvariantCulture);

    /// <summary>The fit the zoom follows, or <see cref="ViewportFit.None"/>.</summary>
    public ViewportFit CurrentFit => Enum.Parse<ViewportFit>(_find().GetAttribute("data-fit")!);

    /// <summary>Clicks the zoom in button. Throws when the zoom is at its largest or the viewport shows no controls.
    /// </summary>
    public async Task ZoomInAsync() => await ZoomAsync("zoom-in");

    /// <summary>Clicks the zoom out button. Throws when the zoom is at its smallest or the viewport shows no
    /// controls.</summary>
    public async Task ZoomOutAsync() => await ZoomAsync("zoom-out");

    private Task ZoomAsync(string part)
    {
        var button = Control(part);
        return button.GetAttribute("aria-disabled") == "true"
            ? throw new InvalidOperationException("The viewport can't zoom any further that way.")
            : button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Picks <see cref="ViewportFit.Width"/> or <see cref="ViewportFit.Page"/> from the zoom menu.</summary>
    public async Task FitAsync(ViewportFit fit)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(fit, ViewportFit.None);
        var (trigger, list) = Menu();
        await TbMenuDriver.ClickAsync(trigger,
            list.QuerySelectorAll("[role=menuitemradio]")[fit == ViewportFit.Width ? 0 : 1]);
    }

    /// <summary>Picks Actual size from the zoom menu, which shows the content at 100%.</summary>
    public async Task ActualSizeAsync()
    {
        var (trigger, list) = Menu();
        await TbMenuDriver.ClickAsync(trigger, list.QuerySelector("[role=menuitem]")!);
    }

    private IElement Control(string part) => _find().QuerySelector($"[data-part=controls] [data-part={part}]")
        ?? throw new InvalidOperationException("The viewport shows no controls; set ShowControls.");

    private (IElement Trigger, IElement List) Menu()
    {
        var trigger = Control("zoom-menu").QuerySelector("[aria-haspopup=menu]")!;
        return (trigger, _findAll($"[id='{trigger.GetAttribute("aria-controls")}']").Single());
    }
}

/// <summary>A <c>TbQueryBuilder</c>, found by its <c>Label</c>. A group is named by its path of group numbers from
/// the outer group, so no numbers is the outer group itself and <c>2, 1</c> is the first group inside the outer
/// group's second group.</summary>
public sealed class TbQueryBuilderDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbQueryBuilderDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll,
        ScriptCalls scripts) => (_find, _findAll, _scripts) = (find, findAll, scripts);

    /// <summary>The fields of a group's condition with this number, found by their labels, Field, Comparison and
    /// Value: <c>Condition(1).Select("Field").ChooseAsync("Region")</c>.</summary>
    public TrailblazorMarkup Condition(int number, params int[] group) =>
        new TrailblazorMarkup(_findAll, _scripts).Within(() => ConditionOf(number, group));

    /// <summary>Clicks the group's Add condition button, which adds a condition on the first field.</summary>
    public async Task AddConditionAsync(params int[] group) =>
        await Click(GroupOf(group), "group-actions", "add-condition");

    /// <summary>Clicks the group's Add group button, which adds a group with one condition inside it. Throws when the
    /// group is at the deepest level.</summary>
    public async Task AddGroupAsync(params int[] group) => await Click(GroupOf(group), "group-actions", "add-group");

    /// <summary>Clicks the remove button of the group's condition with this number.</summary>
    public async Task RemoveConditionAsync(int number, params int[] group) =>
        await ConditionOf(number, group).QuerySelector("[data-part=remove-condition]")!
            .ClickAsync(new MouseEventArgs());

    /// <summary>Clicks the remove button of the group at this path, which can't be the outer group.</summary>
    public async Task RemoveGroupAsync(params int[] group)
    {
        ArgumentOutOfRangeException.ThrowIfZero(group.Length, nameof(group));
        await Click(GroupOf(group), "group-header", "remove-group");
    }

    /// <summary>Picks whether the group matches items that meet all of its conditions or any of them.</summary>
    public async Task JoinAsync(QueryJoin join, params int[] group)
    {
        var select = new TbSelectDriver(() => GroupOf(group).QuerySelector(":scope > [data-part=group-header] "
            + "[role=combobox]")!, _findAll, _scripts);
        await select.ChooseAsync(select.Options[join == QueryJoin.And ? 0 : 1]);
    }

    private static Task Click(IElement group, string part, string button) =>
        (group.QuerySelector($":scope > [data-part={part}] [data-part={button}]") ?? throw new InvalidOperationException(
            $"The query builder's group has no {button.Replace('-', ' ')} button here.")).ClickAsync(new MouseEventArgs());

    private IElement ConditionOf(int number, int[] group) =>
        GroupOf(group).QuerySelectorAll(":scope > [data-part=conditions] > li > [data-part=condition]")
            .ElementAtOrDefault(number - 1) ?? throw new InvalidOperationException(
                $"The query builder's group has no condition {number}.");

    private IElement GroupOf(int[] path)
    {
        var group = _find();
        foreach (var number in path)
        {
            group = group.QuerySelectorAll(":scope > [data-part=groups] > [data-part=group]").ElementAtOrDefault(number - 1)
                ?? throw new InvalidOperationException($"The query builder has no group {string.Join(", ", path)}.");
        }

        return group;
    }
}

/// <summary>A <c>TbMenubar</c>, found by its <c>AriaLabel</c>. Its menus are named by their labels.</summary>
public sealed class TbMenubarDriver
{
    private readonly Func<IElement> _find;
    private readonly ScriptCalls _scripts;

    internal TbMenubarDriver(Func<IElement> find, ScriptCalls scripts) => (_find, _scripts) = (find, scripts);

    /// <summary>The names of the bar's menus, in order.</summary>
    public IReadOnlyList<string> Menus => [.. _find().QuerySelectorAll(":scope > .tb-menu > [data-part=trigger]")
        .Select(Choices.NameOf)];

    /// <summary>The bar's menu with this name, to open it or choose its items, as in
    /// <c>Menu("Edit").ChooseAsync("Undo")</c>. Throws when the bar has no such menu.</summary>
    public TbMenuDriver Menu(string name) => Menus.Contains(name)
        ? new(name, selector => [.. _find().QuerySelectorAll(selector)], _scripts)
        : throw new InvalidOperationException($"The menubar has no menu \"{name}\".");
}

/// <summary>A <c>TbMenu</c>, found by its trigger's name.</summary>
public sealed class TbMenuDriver
{
    internal const string Items = "[role=menuitem]:not([data-part=submenu-trigger]), [role=menuitemcheckbox], "
        + "[role=menuitemradio]";

    private readonly string _trigger;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;
    private readonly ScriptCalls _scripts;

    internal TbMenuDriver(string trigger, Func<string, IReadOnlyList<IElement>> findAll, ScriptCalls scripts) =>
        (_trigger, _findAll, _scripts) = (trigger, findAll, scripts);

    /// <summary>Whether the menu is open. Its items are in the markup either way, so this reads the menu's own state.
    /// </summary>
    public bool IsOpen => ListOf(Trigger()).HasAttribute("data-open");

    /// <summary>Opens the menu, as clicking its trigger does. An open menu is left as it is. Throws for a disabled
    /// menu, or one on a static page, which opens natively, without the script that tells the app it's open.</summary>
    public async Task OpenAsync() => await SetOpenAsync(true);

    /// <summary>Closes the menu, as pressing Escape does. A closed menu is left as it is. Throws for an open menu on
    /// a static page, as <see cref="OpenAsync"/> does.</summary>
    public async Task CloseAsync() => await SetOpenAsync(false);

    /// <summary>Clicks the item with this name, through the submenus named before it, as in
    /// <c>ChooseAsync("Export", "PDF")</c>. A checkbox or radio item is clicked the same way, and a link item also
    /// takes the test's <c>NavigationManager</c> to its address, unless it opens in a new tab. Throws for a disabled
    /// menu or item, a name the menu doesn't have, or a button item on a static page, where only links work.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        var trigger = EnabledTrigger();
        var item = Find(ListAt(trigger, path[..^1]), path[^1], Items, "item");
        await ClickAsync(trigger, item);
        _scripts.Follow(item);
    }

    /// <summary>Whether the checkbox or radio item with this name, through the submenus named before it, is checked.
    /// The items are in the markup whether the menu is open or not, so this reads it either way. Throws for a name the
    /// menu doesn't have as a checkbox or radio item.</summary>
    public bool Checked(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        return Find(ListAt(Trigger(), path[..^1]), path[^1], "[role=menuitemcheckbox], [role=menuitemradio]",
            "checkbox or radio item").GetAttribute("aria-checked") == "true";
    }

    // The list the trigger opens, or the one reached through these submenus.
    private IElement ListAt(IElement trigger, string[] submenus)
    {
        var list = ListOf(trigger);
        foreach (var name in submenus)
        {
            list = ListOf(Find(list, name, "[data-part=submenu-trigger]", "submenu"));
        }

        return list;
    }

    internal static Task ClickAsync(IElement trigger, IElement item)
    {
        if (item.GetAttribute("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"The menu item \"{Choices.NameOf(item)}\" is disabled.");
        }

        if (trigger.HasAttribute("data-static") && item.TagName != "A")
        {
            throw new InvalidOperationException(
                "This menu is on a static page, where only its links work; render it on an interactive page.");
        }

        return item.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    // The script tells the menu it opened or closed; a menu that attaches on first use does so as it's focused.
    private async Task SetOpenAsync(bool open)
    {
        if (IsOpen == open)
        {
            return;
        }

        // Escape still closes a disabled menu that the app opened from code.
        var trigger = open ? EnabledTrigger() : Trigger();
        if (trigger.HasAttribute("data-static"))
        {
            throw new InvalidOperationException($"The menu \"{_trigger}\" is on a static page, where it opens without "
                + "its script; render it on an interactive page.");
        }

        if (trigger.HasAttribute("data-attach-on-use"))
        {
            await trigger.FocusAsync(new FocusEventArgs());
        }

        var events = _scripts.Menu(trigger.GetAttribute("aria-controls"));
        await _scripts.InvokeAsync(() => events.OnToggle(open));
    }

    private IElement EnabledTrigger()
    {
        var trigger = Trigger();
        return trigger.HasAttribute("disabled") || trigger.GetAttribute("aria-disabled") == "true"
            ? throw new InvalidOperationException($"The menu \"{_trigger}\" is disabled.") : trigger;
    }

    private IElement Trigger()
    {
        var triggers = _findAll("[aria-haspopup=menu][data-part=trigger]")
            .Where(button => Choices.NameOf(button) == _trigger).ToList();
        return triggers.Count == 1 ? triggers[0]
            : throw new InvalidOperationException(
                $"Expected one menu trigger named \"{_trigger}\", found {triggers.Count}.");
    }

    private IElement ListOf(IElement opener) =>
        _findAll($"[id='{opener.GetAttribute("aria-controls")}']").SingleOrDefault()
        ?? throw new InvalidOperationException($"The menu that \"{Choices.NameOf(opener)}\" opens isn't in the markup.");

    // A submenu's items sit inside the parent's list, so only the items whose nearest list is this one count.
    internal static IElement Find(IElement list, string name, string selector, string kind)
    {
        var found = list.QuerySelectorAll(selector)
            .Where(item => item.Closest("[role=menu]") == list && Choices.NameOf(item) == name).ToList();
        return found.Count switch
        {
            1 => found[0],
            0 => throw new InvalidOperationException($"The menu has no {kind} named \"{name}\"."),
            _ => throw new InvalidOperationException($"The menu has {found.Count} {kind}s named \"{name}\"."),
        };
    }
}

/// <summary>Drives a <c>TbOverlay</c>, found by its <c>aria-label</c> or as the only one.</summary>
public sealed class TbOverlayDriver
{
    private readonly Func<IReadOnlyList<IElement>> _find;
    private readonly string? _name;
    private readonly ScriptCalls _scripts;

    internal TbOverlayDriver(Func<IReadOnlyList<IElement>> find, string? name, ScriptCalls scripts) =>
        (_find, _name, _scripts) = (find, name, scripts);

    /// <summary>Whether the overlay is open, which is when it renders.</summary>
    public bool IsOpen => _find().Count > 0;

    /// <summary>Clicks the overlay outside its content, as a user does, which raises its <c>OnClick</c> and closes an
    /// overlay with <c>CloseOnClick</c>, and waits until the <c>OnClick</c>, and any close, have run. It throws for a
    /// closed overlay.</summary>
    public async Task ClickAsync() =>
        await Find().QuerySelector("[data-part=scrim]")!.ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Presses Escape, which closes an overlay with <c>CloseOnClick</c>, and waits until it has closed. It
    /// throws for a closed overlay, or one that Escape doesn't close.</summary>
    public async Task PressEscapeAsync()
    {
        var layer = _scripts.Layer(Find())
            ?? throw new InvalidOperationException(
                "Escape doesn't close this overlay; it closes one with CloseOnClick.");
        await _scripts.InvokeAsync(() => layer.OnDismissRequested(Trailblazor.Primitives.DismissReason.EscapeKey));
    }

    private IElement Find()
    {
        var overlays = _find();
        return overlays.Count == 1 ? overlays[0] : throw new InvalidOperationException(overlays.Count == 0
            ? _name is null ? "No overlay is open." : $"No overlay named \"{_name}\" is open."
            : $"{overlays.Count} overlays are open; name the one to use with its aria-label.");
    }
}

/// <summary>A <c>TbCopyButton</c>, found by its words.</summary>
public sealed class TbCopyButtonDriver
{
    private readonly Func<IElement> _find;

    internal TbCopyButtonDriver(Func<IElement> find) => _find = find;

    /// <summary>How its last copy went, while it still shows it: <c>copied</c> or <c>failed</c>; <c>idle</c> before
    /// a copy and once the moment passes.</summary>
    public string State => _find().GetAttribute("data-state")!;

    /// <summary>Presses the button, which copies through the app's <c>IBrowserService</c>. With the kit's
    /// <see cref="FakeBrowserService"/>, <see cref="FakeBrowserService.Copied"/> then lists the text.</summary>
    public async Task CopyAsync() => await _find().ClickAsync(new MouseEventArgs());
}

/// <summary>A <c>TbValidationSummary</c>, which shows only while its form has messages.</summary>
public sealed class TbValidationSummaryDriver
{
    private readonly Func<IReadOnlyList<IElement>> _find;

    internal TbValidationSummaryDriver(Func<IReadOnlyList<IElement>> find) => _find = find;

    /// <summary>The messages it lists, in order, or none while it doesn't show.</summary>
    public IReadOnlyList<string> Messages =>
        [.. _find().SelectMany(summary => summary.QuerySelectorAll("[data-part=item]"))
            .Select(item => item.TextContent.Trim())];

    /// <summary>Presses the link of this message, which moves focus to its field. A message the list doesn't show,
    /// or one that belongs to no field and so has no link, throws.</summary>
    public async Task FollowAsync(string message)
    {
        var link = _find().SelectMany(summary => summary.QuerySelectorAll("[data-part=link]"))
            .FirstOrDefault(link => link.TextContent.Trim() == message)
            ?? throw new InvalidOperationException($"The validation summary has no link \"{message}\"; it lists "
                + string.Join(", ", Messages.Select(shown => $"\"{shown}\"")) + ".");
        await link.ClickAsync(new MouseEventArgs());
    }
}
