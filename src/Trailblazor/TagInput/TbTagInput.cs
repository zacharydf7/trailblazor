using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A field where people type tags, such as a trail's features. Enter or a separator adds what was typed as a
/// tag, shown as a chip with a remove button. Bind it with <c>@bind-Value</c> to an <see cref="IReadOnlyList{T}"/> of
/// strings.</summary>
public partial class TbTagInput : IAsyncDisposable
{
    // Where focus goes once a tag is removed, as the script names it in a remove event.
    private const string FocusPrevious = "previous";
    private const string FocusNext = "next";
    private const string FocusInput = "input";

    private ElementReference _input;
    private IJSObjectReference? _handle;
    private int? _focusAfter;
    private bool _disposed;
    private readonly ItemHandlers<string, Func<MouseEventArgs, Task>> _remove;
    private readonly List<int> _ids = [];
    private List<string> _idsFor = [];
    private int _nextId;

    /// <summary>Creates the tag input.</summary>
    public TbTagInput() => _remove = new(key => args => OnRemoveClickAsync(key, args));

    /// <summary>Example text shown while nothing is typed. It is not a label.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Characters that add what was typed as a tag, as Enter does. Pasted text is split at them and at line
    /// breaks. Defaults to a comma; empty leaves only Enter and line breaks, and then only the first refused part of a
    /// paste goes back into the input.</summary>
    [Parameter]
    public string Separators { get; set; } = ",";

    /// <summary>The most tags the field takes. A tag typed past it stays in the input with a message.</summary>
    [Parameter]
    public int? MaxTags { get; set; }

    /// <summary>Decides whether a typed tag is already there. Defaults to ignoring case in the current culture.
    /// </summary>
    [Parameter]
    public IEqualityComparer<string>? Comparer { get; set; }

    // Comparer only finds duplicates, so a tag typed again in another case is still a change.
    private protected override bool SameValue(IReadOnlyList<string>? value, IReadOnlyList<string>? other) =>
        SameItems(value, other, comparer: null);

    /// <summary>Checks a typed tag before it's added, returning a message to show, or null to add it. A tag with a
    /// message stays in the input for the user to fix.</summary>
    [Parameter]
    public Func<string, string?>? TagValidation { get; set; }

    /// <summary>Replaces the words the field says and shows, such as its hint and its messages.</summary>
    [Parameter]
    public TagInputLabels? Labels { get; set; }

    /// <summary>Extra CSS classes for the field's inner parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) => _input.FocusAsync(preventScroll);

    private protected override string FieldModifiers => ClassNames.Join(base.FieldModifiers, "tb-tag-input");

    private IReadOnlyList<string> Tags => Value ?? [];

    // Tags are added and removed through the script, so a page that isn't interactive only shows them.
    private bool Editable => !Disabled && !ReadOnly && RendererInfo.IsInteractive;

    private string HintId => $"{FieldId}-hint";

    private string? Described => !Editable ? InputDescribedBy
        : InputDescribedBy is { } described ? $"{described} {HintId}" : HintId;

    private string RemoveFormat => Labels?.Remove ?? Localizer[TbStrings.TagInputRemove];

    private static string Format(string format, object? arg) => string.Format(CultureInfo.CurrentCulture, format, arg);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        _remove.KeepOnly(_ids.Select(id => id.ToString(CultureInfo.InvariantCulture)));
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        try
        {
            if (firstRender)
            {
                var module = await Modules.ImportAsync("./_content/Trailblazor/tag-input.js");
                var handle = await module.InvokeAsync<IJSObjectReference?>("createTagInput", _input);
                if (_disposed && handle is not null)
                {
                    await handle.DisposeHandleAsync();
                    return;
                }

                _handle = handle;
            }

            if (_focusAfter is { } index && _handle is not null)
            {
                _focusAfter = null;
                await _handle.InvokeVoidAsync("focusTag", index);
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the field with it.
        }
    }

    // Each part, split at the separators and line breaks, is added unless it's blank, already there, past the limit or
    // refused by the check. What's refused goes back into the input with the first refusal's message; with nothing
    // refused, an earlier message is taken back.
    private async Task OnAddAsync(TagAddEventArgs e)
    {
        if (!Editable)
        {
            return;
        }

        var tags = Tags.ToList();
        var added = new List<string>();
        var refused = new List<string>();
        string? error = null;
        var parts = e.Text.Split([.. Separators, '\r', '\n']).Select(text => text.Trim());
        foreach (var text in parts.Where(text => text.Length > 0))
        {
            if (Refusal(tags, text) is { } problem)
            {
                refused.Add(text);
                error ??= problem;
                continue;
            }

            tags.Add(text);
            added.Add(text);
        }

        if (added.Count > 0)
        {
            Telemetry.Used(TelemetryName, "add");
            SyncIds();
            _ids.AddRange(added.Select(_ => _nextId++));
            _idsFor = [.. tags];
            await SetValueAsync(tags);
            Announcer.Polite(string.Join(", ", added.Select(tag => Format(Labels?.Added
                ?? Localizer[TbStrings.TagInputAdded], tag))));
        }

        if (error is null)
        {
            ClearParseError();
            return;
        }

        ShowParseError(error, null);
        if (_handle is not null && !_disposed)
        {
            await _handle.InvokeVoidAsync("restore", refused);
        }
    }

    private string? Refusal(List<string> tags, string text)
    {
        if (tags.Contains(text, Comparer ?? StringComparer.CurrentCultureIgnoreCase))
        {
            return Format(Labels?.Duplicate ?? Localizer[TbStrings.TagInputDuplicate], text);
        }

        if (MaxTags is int most && tags.Count >= most)
        {
            return Format(Labels?.TooMany ?? Localizer.Plural(TbStrings.TagInputTooMany, most), most);
        }

        return TagValidation?.Invoke(text);
    }

    // A click with the pointer goes back to typing; Enter or Space on the button moves on to the next tag. The tag is
    // looked up by its key when the click arrives, since an earlier removal can shift its position.
    private Task OnRemoveClickAsync(string key, MouseEventArgs e) =>
        RemoveAsync(IndexOf(key), e.Detail > 0 ? FocusInput : FocusNext);

    // A tag keeps its id through the field's own changes, since a bound value can repeat a tag's text. Any other change
    // to the tags, such as a value the app replaced or changed in place, gives every tag a new id.
    private void SyncIds()
    {
        if (!Tags.SequenceEqual(_idsFor))
        {
            _ids.Clear();
            _ids.AddRange(Tags.Select(_ => _nextId++));
            _idsFor = [.. Tags];
        }
    }

    private string KeyOf(int at) => _ids[at].ToString(CultureInfo.InvariantCulture);

    private int IndexOf(string key)
    {
        SyncIds();
        return Enumerable.Range(0, Tags.Count).FirstOrDefault(at => KeyOf(at) == key, -1);
    }

    private Task OnRemoveKeyAsync(TagRemoveEventArgs e) => RemoveAsync(IndexOf(e.Key), e.Focus);

    private async Task RemoveAsync(int index, string focus)
    {
        if (!Editable || index < 0 || index >= Tags.Count)
        {
            return;
        }

        var tag = Tags[index];
        var tags = Tags.Where((_, at) => at != index).ToList();
        Telemetry.Used(TelemetryName, "remove");
        _ids.RemoveAt(index);
        _idsFor = [.. tags];
        await SetValueAsync(tags);
        Announcer.Polite(Format(Labels?.Removed ?? Localizer[TbStrings.TagInputRemoved], tag));
        // -1 is the input, which takes focus when no tag is left in that direction.
        _focusAfter = focus switch
        {
            FocusPrevious => index > 0 ? index - 1 : tags.Count > 0 ? 0 : -1,
            FocusNext => index < tags.Count ? index : -1,
            _ => -1,
        };
        StateHasChanged();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync when both exist, so the field's own cleanup has to run from here.
        Dispose();
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }
    }
}
