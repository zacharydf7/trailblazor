using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Trailblazor;

/// <summary>Chooses files by clicking or dropping them on a drop zone, and lists them with a way to remove each.
/// Bind it with <c>@bind-Value</c> to the chosen <see cref="IBrowserFile"/>s, and read them with
/// <c>OpenReadStream</c> before the next choice replaces them.</summary>
public partial class TbFileUpload
{
    private readonly List<string> _rejected = [];
    private InputFile? _input;
    // Counts the choices taken, which tests wait on.
    private int _choices;
    // InputFile hears the browser only after its first interactive render, so tests wait for this before choosing.
    private bool _ready;
    private bool _dragging;

    /// <summary>The file types to take, as the input's <c>accept</c> attribute lists them, such as
    /// <c>.pdf,image/*</c>. Dropped files are checked against it too, since browsers don't.</summary>
    [Parameter]
    public string? Accept { get; set; }

    /// <summary>Takes more than one file at a time.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>The most files a choice keeps; the rest are left out with a message.</summary>
    [Parameter]
    public int? MaxFiles { get; set; }

    /// <summary>The largest file, in bytes, the field takes. Defaults to 512,000, the size
    /// <c>OpenReadStream</c> reads by default.</summary>
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

    /// <summary>The words the field shows and says to screen readers.</summary>
    [Parameter]
    public FileUploadLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the field's parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    private IReadOnlyList<IBrowserFile> Files => Value ?? [];

    private string? AccessibleName => Label is null ? Labels.Choose : null;

    private string RejectedId => $"{FieldId}-rejected";

    private string? Description => _rejected.Count == 0 ? InputDescribedBy
        : string.Join(" ", new[] { InputDescribedBy, RejectedId }.OfType<string>());

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive)
        {
            // InputFile starts listening in its own first after-render, which runs after this one, so it goes first.
            await Task.Yield();
            _ready = true;
            StateHasChanged();
        }
    }

    private async Task OnChangeAsync(InputFileChangeEventArgs args)
    {
        _dragging = false;
        _choices++;
        _rejected.Clear();
        var chosen = new List<IBrowserFile>();
        var tooMany = false;
        foreach (var file in args.GetMultipleFiles(args.FileCount))
        {
            if (!Accepts(file))
            {
                _rejected.Add(string.Format(CultureInfo.CurrentCulture, Labels.WrongType, file.Name, Accept));
            }
            else if (file.Size > MaxFileSize)
            {
                _rejected.Add(
                    string.Format(CultureInfo.CurrentCulture, Labels.TooLarge, file.Name, FormatSize(MaxFileSize)));
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
            _rejected.Add(most == 1 ? Labels.TooManyOne
                : string.Format(CultureInfo.CurrentCulture, Labels.TooMany, most));
        }

        Announcer.Polite(string.Join(" ", [Count(chosen.Count), .. _rejected]));
        await SetValueAsync(chosen.Count == 0 ? null : chosen);
    }

    private async Task RemoveAsync(IBrowserFile file)
    {
        var left = Files.Where(other => other != file).ToList();
        await SetValueAsync(left.Count == 0 ? null : left);
        await FocusInputAsync();
    }

    private async Task ClearAsync()
    {
        _rejected.Clear();
        await SetValueAsync(null);
        await FocusInputAsync();
    }

    // The button that was pressed goes with its file, so focus moves to the input rather than the page.
    private async Task FocusInputAsync()
    {
        if (_input?.Element is { } input)
        {
            await input.FocusAsync();
        }
    }

    // An accept list names extensions such as .pdf, types such as application/pdf, or families such as image/*.
    private bool Accepts(IBrowserFile file) => string.IsNullOrWhiteSpace(Accept) || Accept
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(type => type.StartsWith('.') ? file.Name.EndsWith(type, StringComparison.OrdinalIgnoreCase)
            : type.EndsWith("/*", StringComparison.Ordinal)
                ? file.ContentType.StartsWith(type[..^1], StringComparison.OrdinalIgnoreCase)
                : string.Equals(file.ContentType, type, StringComparison.OrdinalIgnoreCase));

    private string Count(int files) =>
        files == 1 ? Labels.ChosenOne : string.Format(CultureInfo.CurrentCulture, Labels.Chosen, files);

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1_000 => $"{bytes} B",
        < 1_000_000 => $"{bytes / 1_000d:0.#} KB",
        < 1_000_000_000 => $"{bytes / 1_000_000d:0.#} MB",
        _ => $"{bytes / 1_000_000_000d:0.#} GB",
    };
}
