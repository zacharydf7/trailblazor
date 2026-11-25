using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>Chooses files by clicking or dropping them on a drop zone, and lists them with a way to remove each.
/// Bind it with <c>@bind-Value</c> to the chosen <see cref="IBrowserFile"/>s, and read them with
/// <c>OpenReadStream</c> before the next choice replaces them.</summary>
public partial class TbFileUpload
{
    private readonly List<string> _rejected = [];
    // Each chosen file's upload, by the file the value holds.
    private readonly Dictionary<IBrowserFile, FileUploadItem> _items = new(ReferenceEqualityComparer.Instance);
    // How many OnUploaded handlers are still running.
    private int _handling;
    private ListeningInputFile? _input;
    // InputFile hears the browser only after its first interactive render, so tests wait for this before choosing.
    private bool _ready;
    private bool _dragging;

    /// <summary>The file types to take, as the input's <c>accept</c> attribute lists them, such as
    /// <c>.pdf,image/*</c>. Without it, an <c>accept</c> in <c>InputAttributes</c> is used. The picker shows only those
    /// types, and a chosen or dropped file of another type is left out with a message, since browsers don't check
    /// drops; set <see cref="RejectOtherTypes"/> to false to take it and check it yourself.</summary>
    [Parameter]
    public string? Accept { get; set; }

    /// <summary>Leaves out a chosen or dropped file of a type the accept list doesn't name, with a message. Defaults
    /// to true; false only filters the picker, and a file of another type is taken like any other, within
    /// <see cref="MaxFileSize"/> and <see cref="MaxFiles"/>.</summary>
    [Parameter]
    public bool RejectOtherTypes { get; set; } = true;

    /// <summary>Takes more than one file at a time.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>The most files a choice keeps; the rest are left out with a message.</summary>
    [Parameter]
    public int? MaxFiles { get; set; }

    /// <summary>The largest file, in bytes, the field takes; a larger one is left out with a message. Defaults to
    /// 512,000 (512 KB), the size <c>OpenReadStream</c> reads by default, so for larger files raise both this and the
    /// <c>maxAllowedSize</c> you pass to <c>OpenReadStream</c>.</summary>
    [Parameter]
    public long MaxFileSize { get; set; } = 512_000;

    /// <summary>Shows a drop zone. Without it, the field is a button, for picking a single settings or project file.
    /// Defaults to true.</summary>
    [Parameter]
    public bool DropZone { get; set; } = true;

    /// <summary>Content for the drop zone instead of its text, such as an icon and a hint about the files to drop.
    /// </summary>
    [Parameter]
    public RenderFragment? ZoneContent { get; set; }

    /// <summary>Shows nothing on the page but the reasons any files were left out and its messages, for a field opened
    /// only from code with <see cref="OpenAsync"/>, such as by an "Open file" menu item. Its files arrive through
    /// <c>ValueChanged</c> as usual.</summary>
    [Parameter]
    public bool Hidden { get; set; }

    private const string Script = "./_content/Trailblazor/file-upload.js";

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    /// <summary>The words the field shows and says to screen readers.</summary>
    [Parameter]
    public FileUploadLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the field's parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    private IReadOnlyList<IBrowserFile> Files => Value ?? [];

    private string? AccessibleName => ShownLabel is null ? ChooseLabel : null;

    private string RejectedId => $"{FieldId}-rejected";

    private string? Description => _rejected.Count == 0 ? InputDescribedBy
        : string.Join(" ", new[] { InputDescribedBy, RejectedId }.OfType<string>());

    /// <summary>Opens the browser's file picker, as clicking the field does. Browsers open it only during the user's
    /// own click or key press, so call it from that event's handler, such as a menu item's <c>OnClick</c>, and not
    /// after a delay. Does nothing while the field is disabled or read only.</summary>
    public async Task OpenAsync()
    {
        if (Disabled || ReadOnly || _input?.Element is not { } input)
        {
            return;
        }

        var module = await Modules.ImportAsync(Script);
        await module.InvokeVoidAsync("openPicker", input);
    }

    /// <summary>Empties the field and raises <c>ValueChanged</c>, as its clear button does, without moving focus.
    /// </summary>
    public async Task ClearAsync()
    {
        _rejected.Clear();
        Forget([]);
        await SetValueAsync(null);
        StateHasChanged();
        await ClearPickerAsync();
    }

    // Removing the last file sets the value to null, which [Required] rejects.
    private protected override bool ClearsToEmptyList => false;

    private protected override async Task OnResetAsync()
    {
        _rejected.Clear();
        Forget(Files);
        await ClearPickerAsync();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // A value the app sets stops the uploads of the files it leaves out.
        Forget(Files);
    }

    private async Task ClearPickerAsync()
    {
        if (_input?.Element is { } input)
        {
            var module = await Modules.ImportAsync(Script);
            await module.InvokeVoidAsync("clearPicker", input);
        }
    }

    // Built here because its component is internal; InputFile puts the attributes it doesn't take on the input.
    private RenderFragment Input => builder =>
    {
        builder.OpenComponent<ListeningInputFile>(0);
        builder.AddMultipleAttributes(1, ControlAttributes);
        builder.AddAttribute(2, "data-part", "input");
        builder.AddAttribute(3, "id", InputId);
        builder.AddAttribute(4, "name", InputNameAttribute);
        builder.AddComponentParameter(5, nameof(InputFile.OnChange),
            EventCallback.Factory.Create<InputFileChangeEventArgs>(this, OnChangeAsync));
        builder.AddAttribute(6, "accept", AcceptList);
        builder.AddAttribute(7, "multiple", Multiple);
        builder.AddAttribute(8, "disabled", Disabled || ReadOnly);
        builder.AddAttribute(9, "aria-label", AccessibleName);
        builder.AddAttribute(10, "aria-required", AriaRequired);
        builder.AddAttribute(11, "aria-invalid", ShowsInvalid ? "true" : null);
        builder.AddAttribute(12, "aria-describedby", Description);
        builder.AddAttribute(13, "data-static", AssignedRenderMode is null && !RendererInfo.IsInteractive ? "" : null);
        builder.AddAttribute(14, "data-prerender",
            AssignedRenderMode is not null && !RendererInfo.IsInteractive ? "" : null);
        builder.AddAttribute(16, "data-ready", _ready ? "" : null);
        builder.AddAttribute(17, "tabindex", Hidden ? "-1" : null);
        builder.AddComponentParameter(18, nameof(ListeningInputFile.OnListening),
            EventCallback.Factory.Create(this, () => _ready = true));
        builder.AddAttribute(19, "ondragenter", EventCallback.Factory.Create(this, () => _dragging = true));
        builder.AddAttribute(20, "ondragleave", EventCallback.Factory.Create(this, () => _dragging = false));
        builder.AddAttribute(21, "ondrop", EventCallback.Factory.Create(this, () => _dragging = false));
        builder.AddAttribute(22, "class", ClassNames.Join("tb-file-upload__input", Classes?.Input));
        builder.AddComponentReferenceCapture(23, component => _input = (ListeningInputFile)component);
        builder.CloseComponent();
    };

    /// <summary>Sends a chosen file, such as to storage or an API: read it with <c>OpenReadStream</c>, report the bytes
    /// sent so far to the <see cref="IProgress{T}"/>, and stop when the token is canceled. With it, the field shows
    /// each file's progress, a button that cancels it while it runs, and a retry after it fails or is canceled; a
    /// failure is logged as a warning, and counted in <c>trailblazor.control.failed</c> when the field has a
    /// <c>TelemetryName</c>. Without it, the field only chooses files, and the app sends them.</summary>
    [Parameter]
    public Func<IBrowserFile, IProgress<long>, CancellationToken, Task>? Upload { get; set; }

    /// <summary>Raised with each file <see cref="Upload"/> has sent, such as to list what has been uploaded. Unlike
    /// <see cref="Upload"/>, it renders the page again.</summary>
    [Parameter]
    public EventCallback<IBrowserFile> OnUploaded { get; set; }

    /// <summary>Sends files with <see cref="Upload"/> as soon as they are chosen. Defaults to true; false shows an
    /// Upload button, and <see cref="UploadAsync"/> sends them from code.</summary>
    [Parameter]
    public bool AutoUpload { get; set; } = true;

    /// <summary>How many files <see cref="Upload"/> sends at a time, at least 1. Defaults to 3; the others wait their
    /// turn.</summary>
    [Parameter]
    public int MaxConcurrent { get; set; } = 3;

    /// <summary>What a chosen file shows in place of its name and size, such as a thumbnail from
    /// <c>RequestImageFileAsync</c>. The field's own progress bar, buttons and status still follow it.</summary>
    [Parameter]
    public RenderFragment<FileUploadItem>? FileTemplate { get; set; }

    /// <summary>Sends the chosen files that haven't been sent, as the Upload button does when
    /// <see cref="AutoUpload"/> is off. Does nothing without <see cref="Upload"/>.</summary>
    public Task UploadAsync()
    {
        StartWaiting();
        return Task.CompletedTask;
    }

    // Whether a chosen file hasn't been sent or started yet, for the Upload button.
    private bool HasWaiting => Files.Any(file => Item(file) is { Status: FileUploadStatus.Chosen, Stop: null });

    // Whether any file is being sent or waiting its turn, or OnUploaded is running, which the testing helpers wait on.
    private bool Busy => _handling > 0 || _items.Values.Any(item => item.Status == FileUploadStatus.Uploading
        || item.Status == FileUploadStatus.Chosen && item.Stop is not null);

    private FileUploadItem Item(IBrowserFile file)
    {
        if (!_items.TryGetValue(file, out var item))
        {
            item = new FileUploadItem(file);
            _items[file] = item;
        }

        return item;
    }

    // Starts waiting files until MaxConcurrent run at once.
    private void StartWaiting()
    {
        if (Upload is null)
        {
            return;
        }

        foreach (var file in Files)
        {
            var item = Item(file);
            if (item.Status == FileUploadStatus.Chosen && item.Stop is null)
            {
                item.Stop = new CancellationTokenSource();
            }
        }

        var running = _items.Values.Count(item => item.Status == FileUploadStatus.Uploading);
        foreach (var item in Files.Select(Item).Where(item => item.Status == FileUploadStatus.Chosen && item.Stop is not null)
            .Take(Math.Max(1, MaxConcurrent) - running).ToList())
        {
            _ = SendAsync(item);
        }
    }

    private async Task SendAsync(FileUploadItem item)
    {
        item.Status = FileUploadStatus.Uploading;
        item.Sent = 0;
        var stop = item.Stop!;
        StateHasChanged();
        var sent = false;
        try
        {
            await Upload!(item.File, new Reporter(this, item), stop.Token);
            sent = true;
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            item.Status = FileUploadStatus.Canceled;
        }
        catch (Exception exception)
        {
            item.Status = FileUploadStatus.Failed;
            Announcer.Polite(Named(Labels.FileFailed, TbStrings.FileUploadFileFailed, item.File.Name));
            Telemetry.Failed(TelemetryName, Label ?? AriaLabel, "upload", exception);
        }
        finally
        {
            item.Stop = null;
            stop.Dispose();
        }

        if (sent)
        {
            (item.Status, item.Sent) = (FileUploadStatus.Uploaded, item.File.Size);
            Announcer.Polite(Named(Labels.FileUploaded, TbStrings.FileUploadFileUploaded, item.File.Name));
            // Counted before the render below, so the field shows busy until the app's handler has run.
            _handling++;
        }

        if (IsDisposed)
        {
            return;
        }

        StartWaiting();
        StateHasChanged();
        if (sent)
        {
            await RaiseUploadedAsync(item.File);
        }
    }

    // The field stays busy until the app's handler has run, so the testing helpers' wait covers it too. Nothing awaits
    // the send, so an error in the handler goes to the renderer as an event's would.
    private async Task RaiseUploadedAsync(IBrowserFile file)
    {
        try
        {
            await OnUploaded.InvokeAsync(file);
        }
        catch (Exception exception)
        {
            await DispatchExceptionAsync(exception);
        }
        finally
        {
            _handling--;
        }

        if (!IsDisposed)
        {
            StateHasChanged();
        }
    }

    // A file's first button cancels its upload while it is sent, retries it after it fails or is canceled, and
    // otherwise removes it. Being one button, it keeps the focus as the file's upload starts and ends.
    private Task PressAsync(FileUploadItem item)
    {
        switch (item.Status)
        {
            case FileUploadStatus.Uploading:
                item.Stop?.Cancel();
                return Task.CompletedTask;
            case FileUploadStatus.Failed or FileUploadStatus.Canceled:
                item.Status = FileUploadStatus.Chosen;
                item.Stop = new CancellationTokenSource();
                StartWaiting();
                return Task.CompletedTask;
            default:
                return RemoveAsync(item.File);
        }
    }

    private (Icon Icon, string Part, string Label) Button(FileUploadItem item) => item.Status switch
    {
        FileUploadStatus.Uploading =>
            (BuiltInIcons.Close, "cancel", Named(Labels.Cancel, TbStrings.FileUploadCancel, item.File.Name)),
        FileUploadStatus.Failed or FileUploadStatus.Canceled =>
            (BuiltInIcons.RotateLeft, "retry", Named(Labels.Retry, TbStrings.FileUploadRetry, item.File.Name)),
        _ => (BuiltInIcons.Close, "remove", string.Format(CultureInfo.CurrentCulture, RemoveLabel, item.File.Name)),
    };

    // A file that leaves the field stops its upload.
    private void Forget(IEnumerable<IBrowserFile> kept)
    {
        var keep = kept.ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var gone in _items.Keys.Where(file => !keep.Contains(file)).ToList())
        {
            _items[gone].Stop?.Cancel();
            _items.Remove(gone);
        }
    }

    private protected override void OnDisposed()
    {
        foreach (var item in _items.Values)
        {
            item.Stop?.Cancel();
        }
    }

    // Reports bytes sent from wherever the app's upload runs, and renders only when the shown percent moves.
    private sealed class Reporter(TbFileUpload field, FileUploadItem item) : IProgress<long>
    {
        public void Report(long value) => _ = field.InvokeAsync(() =>
        {
            if (item.Status != FileUploadStatus.Uploading)
            {
                return;
            }

            var before = Percent(item);
            item.Sent = value;
            if (Percent(item) != before)
            {
                field.StateHasChanged();
            }
        });
    }

    private static int Percent(FileUploadItem item) =>
        item.File.Size <= 0 ? 0 : (int)Math.Clamp(100 * item.Sent / item.File.Size, 0, 100);

    private string Named(string? label, string key, string name) =>
        string.Format(CultureInfo.CurrentCulture, label ?? Localizer[key], name);

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) =>
        _input!.Element!.Value.FocusAsync(preventScroll);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && RendererInfo.IsInteractive)
        {
            // Loaded ahead, so OpenAsync is one call within the click's time limit; a failed load retries on that call.
            _ = Modules.ImportAsync(Script);
        }
    }

    private async Task OnChangeAsync(InputFileChangeEventArgs args)
    {
        _dragging = false;
        _rejected.Clear();
        var chosen = new List<IBrowserFile>();
        var tooMany = false;
        foreach (var file in args.GetMultipleFiles(args.FileCount))
        {
            if (RejectOtherTypes && !Accepts(file))
            {
                _rejected.Add(string.Format(CultureInfo.CurrentCulture, WrongTypeLabel, file.Name, AcceptList));
            }
            else if (file.Size > MaxFileSize)
            {
                _rejected.Add(
                    string.Format(CultureInfo.CurrentCulture, TooLargeLabel, file.Name, FormatSize(MaxFileSize)));
            }
            else if (chosen.Count >= (Multiple ? MaxFiles ?? int.MaxValue : 1))
            {
                tooMany = true;
            }
            else
            {
                chosen.Add(file);
            }
        }

        if (tooMany)
        {
            var most = Multiple ? MaxFiles ?? 1 : 1;
            _rejected.Add(Counted(most == 1 ? Labels.TooManyOne : Labels.TooMany, TbStrings.FileUploadTooMany, most));
        }

        Announcer.Polite(string.Join(" ", [Count(chosen.Count), .. _rejected]));
        Forget(chosen);
        await SetValueAsync(chosen.Count == 0 ? null : chosen);
        if (AutoUpload)
        {
            StartWaiting();
        }
    }

    // The button that was pressed goes with its file, so focus moves to the input rather than the page.
    private async Task RemoveAsync(IBrowserFile file)
    {
        var left = Files.Where(other => other != file).ToList();
        Forget(left);
        await SetValueAsync(left.Count == 0 ? null : left);
        // Emptying the input lets the file be dropped again, and Blazor keeps the others by id, so none are lost.
        await ClearPickerAsync();
        await FocusAsync();
    }

    private async Task OnClearClickedAsync()
    {
        await ClearAsync();
        await FocusAsync();
    }

    // Accept, or else an accept given in InputAttributes, which is honored just as a name given there is.
    private string? AcceptList => Accept ?? InputAttributes?.GetValueOrDefault("accept") as string;

    // An accept list names extensions such as .pdf, types such as application/pdf, or families such as image/*.
    private bool Accepts(IBrowserFile file) => string.IsNullOrWhiteSpace(AcceptList) || AcceptList
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(type => type.StartsWith('.') ? file.Name.EndsWith(type, StringComparison.OrdinalIgnoreCase)
            : type.EndsWith("/*", StringComparison.Ordinal)
                ? file.ContentType.StartsWith(type[..^1], StringComparison.OrdinalIgnoreCase)
                : string.Equals(file.ContentType, type, StringComparison.OrdinalIgnoreCase));

    private string Count(int files) =>
        Counted(files == 1 ? Labels.ChosenOne : Labels.Chosen, TbStrings.FileUploadChosen, files);

    private string Counted(string? label, string key, int count) =>
        string.Format(CultureInfo.CurrentCulture, label ?? Localizer.Plural(key, count), count);

    private string FormatSize(long bytes) => bytes switch
    {
        < 1_000 => SizeText(Labels.Bytes ?? Localizer[TbStrings.FileUploadBytes], bytes),
        < 1_000_000 => SizeText(Labels.Kilobytes ?? Localizer[TbStrings.FileUploadKilobytes], bytes / 1_000d),
        < 1_000_000_000 => SizeText(Labels.Megabytes ?? Localizer[TbStrings.FileUploadMegabytes], bytes / 1_000_000d),
        _ => SizeText(Labels.Gigabytes ?? Localizer[TbStrings.FileUploadGigabytes], bytes / 1_000_000_000d),
    };

    private static string SizeText(string template, double amount) => string.Format(CultureInfo.CurrentCulture,
        template, amount.ToString("0.#", CultureInfo.CurrentCulture));

    private string ZoneLabel => Labels.Zone ?? Localizer[TbStrings.FileUploadZone];
    private string UploadButtonLabel => Labels.Upload ?? Localizer[TbStrings.FileUploadUpload];
    private string ChooseLabel => Labels.Choose ?? Localizer[TbStrings.FileUploadChoose];
    private string RemoveLabel => Labels.Remove ?? Localizer[TbStrings.FileUploadRemove];
    private string ClearLabel => Labels.Clear ?? Localizer[TbStrings.FileUploadClear];
    private string WrongTypeLabel => Labels.WrongType ?? Localizer[TbStrings.FileUploadWrongType];
    private string TooLargeLabel => Labels.TooLarge ?? Localizer[TbStrings.FileUploadTooLarge];
}
