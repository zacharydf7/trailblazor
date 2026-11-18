using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Trailblazor;

// TbRichTextEditor's image dialog: a file, its alt text or Decorative, and the app's upload on Insert. It returns the
// image to put in. C#, so it can stay internal.
internal sealed class RichTextImageDialog : TbDialogContent<RichTextImage>, IDisposable
{
    private readonly CancellationTokenSource _closed = new();
    private IReadOnlyList<IBrowserFile>? _files;
    private string? _alt;
    private bool _decorative;
    private bool _uploading;
    private string? _error;

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [Parameter]
    public Func<IBrowserFile, CancellationToken, Task<string>> Upload { get; set; } = default!;

    [Parameter]
    public long MaxFileSize { get; set; }

    [Parameter]
    public RichTextSchema Schema { get; set; } = RichTextSchema.Default;

    [Parameter]
    public RichTextEditorLabels Labels { get; set; } = new();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbForm>(0);
        builder.AddComponentParameter(1, nameof(TbForm.OnValidSubmit),
            EventCallback.Factory.Create<EditContext>(this, InsertAsync));
        builder.AddComponentParameter(2, nameof(TbForm.Disabled), _uploading);
        builder.AddComponentParameter(3, nameof(TbForm.ChildContent), (RenderFragment)BuildForm);
        builder.CloseComponent();
    }

    private void BuildForm(RenderTreeBuilder builder)
    {
        if (_error is not null)
        {
            builder.OpenComponent<TbAlert>(0);
            builder.AddComponentParameter(1, nameof(TbAlert.Tone), Tone.Danger);
            builder.AddComponentParameter(2, nameof(TbAlert.ChildContent), Text(_error));
            builder.CloseComponent();
        }

        builder.OpenComponent<TbFileUpload>(10);
        builder.AddComponentParameter(11, nameof(TbFileUpload.Label),
            Labels.ImageFile ?? Localizer[TbStrings.RichTextEditorImageFile]);
        builder.AddComponentParameter(12, nameof(TbFileUpload.Accept), "image/*");
        builder.AddComponentParameter(13, nameof(TbFileUpload.MaxFileSize), MaxFileSize);
        builder.AddComponentParameter(14, nameof(TbFileUpload.Required), true);
        builder.AddComponentParameter(15, nameof(TbFileUpload.Value), _files);
        builder.AddComponentParameter(16, nameof(TbFileUpload.ValueChanged),
            EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>?>(this, files => _files = files));
        builder.CloseComponent();

        builder.OpenComponent<TbTextField<string>>(20);
        builder.AddComponentParameter(21, nameof(TbTextField<string>.Label),
            Labels.ImageAlt ?? Localizer[TbStrings.RichTextEditorImageAlt]);
        builder.AddComponentParameter(22, nameof(TbTextField<string>.HelperText),
            Labels.ImageAltHelp ?? Localizer[TbStrings.RichTextEditorImageAltHelp]);
        builder.AddComponentParameter(23, nameof(TbTextField<string>.Value), _alt);
        builder.AddComponentParameter(24, nameof(TbTextField<string>.ValueChanged),
            EventCallback.Factory.Create<string?>(this, alt => _alt = alt));
        builder.AddComponentParameter(25, nameof(TbTextField<string>.UpdateOnInput), true);
        // A decorative image has no alt text, so the field turns off rather than asking for one.
        builder.AddComponentParameter(26, nameof(TbTextField<string>.Required), !_decorative);
        builder.AddComponentParameter(27, nameof(TbTextField<string>.Disabled), _decorative);
        builder.CloseComponent();

        builder.OpenComponent<TbCheckbox<bool>>(30);
        builder.AddComponentParameter(31, nameof(TbCheckbox<bool>.Label),
            Labels.Decorative ?? Localizer[TbStrings.RichTextEditorImageDecorative]);
        builder.AddComponentParameter(32, nameof(TbCheckbox<bool>.HelperText),
            Labels.DecorativeHelp ?? Localizer[TbStrings.RichTextEditorImageDecorativeHelp]);
        builder.AddComponentParameter(33, nameof(TbCheckbox<bool>.Value), _decorative);
        builder.AddComponentParameter(34, nameof(TbCheckbox<bool>.ValueChanged),
            EventCallback.Factory.Create<bool>(this, decorative => _decorative = decorative));
        builder.CloseComponent();

        builder.OpenComponent<TbDialogActions>(40);
        builder.AddComponentParameter(41, nameof(TbDialogActions.ChildContent), (RenderFragment)(actions =>
        {
            DialogButtons.Add(actions, 0, this, Labels.CancelImage ?? Localizer[TbStrings.DialogCancel],
                Variant.Outlined, Tone.Neutral, Dialog.CancelAsync);
            actions.OpenComponent<TbButton>(10);
            actions.AddComponentParameter(11, nameof(TbButton.Type), ButtonType.Submit);
            actions.AddComponentParameter(12, nameof(TbButton.Loading), _uploading);
            actions.AddComponentParameter(13, nameof(TbButton.ChildContent),
                Text(Labels.ApplyImage ?? Localizer[TbStrings.RichTextEditorApplyImage]));
            actions.CloseComponent();
        }));
        builder.CloseComponent();
    }

    // The app stores the file and returns its URL, which the editor must allow before the image goes in.
    private async Task InsertAsync()
    {
        (_uploading, _error) = (true, null);
        try
        {
            var url = RichTextSchema.CleanLink(await Upload(_files![0], _closed.Token));
            if (url is null || !Schema.AllowsImage(url))
            {
                _error = Labels.ImageNotAllowed ?? Localizer[TbStrings.RichTextEditorImageNotAllowed];
                return;
            }

            await Dialog.CloseAsync(new RichTextImage(url, _decorative ? "" : _alt?.Trim() ?? ""));
        }
        // Cut short only when the dialog goes away, with nothing left to show.
        catch (Exception) when (_closed.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            // Shown in the dialog rather than rethrown, which would end a Blazor Server circuit.
            Services.GetService<ILogger<TbRichTextEditor>>()?.LogWarning(exception, "The image couldn't be stored.");
            _error = Labels.ImageFailed ?? Localizer[TbStrings.RichTextEditorImageFailed];
        }
        finally
        {
            _uploading = false;
        }
    }

    private static RenderFragment Text(string text) => content => content.AddContent(0, text);

    public void Dispose() => _closed.Cancel();
}
