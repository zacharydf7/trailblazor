using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// What the link dialog returns: the URL to put on the text, or null to take the link off, and the text to insert.
internal sealed record RichTextLinkEdit(string? Url, string? Text);

// TbRichTextEditor's link dialog. C#, so it can stay internal.
internal sealed partial class RichTextLinkDialog : TbDialogContent<RichTextLinkEdit>
{
    private static readonly IReadOnlyDictionary<string, object> AddressAttributes = new Dictionary<string, object>
    {
        ["inputmode"] = "url", ["autocapitalize"] = "none", ["spellcheck"] = "false",
    };

    private string? _text;
    private string? _address;

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Parameter]
    public string? Link { get; set; }

    [Parameter]
    public bool Insert { get; set; }

    [Parameter]
    public RichTextSchema Schema { get; set; } = RichTextSchema.Default;

    [Parameter]
    public RichTextEditorLabels Labels { get; set; } = new();

    // A bare address such as example.com/page would be a relative link, so it gets https:// as an address bar would,
    // and an email address gets mailto:.
    internal static string Complete(string? address)
    {
        var link = RichTextSchema.CleanLink(address) ?? "";
        return HostName().IsMatch(link) ? "https://" + link : EmailAddress().IsMatch(link) ? "mailto:" + link : link;
    }

    protected override void OnInitialized() => _address = Link;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbForm>(0);
        builder.AddComponentParameter(1, nameof(TbForm.OnValidSubmit),
            EventCallback.Factory.Create<EditContext>(this, () => Dialog.CloseAsync(new(Complete(_address), _text))));
        builder.AddComponentParameter(2, nameof(TbForm.ChildContent), (RenderFragment)BuildForm);
        builder.CloseComponent();
    }

    private void BuildForm(RenderTreeBuilder builder)
    {
        if (Insert)
        {
            AddField(builder, 0, Labels.LinkText ?? Localizer[TbStrings.RichTextEditorLinkText], _text,
                text => _text = text);
        }

        AddField(builder, 10, Labels.LinkAddress ?? Localizer[TbStrings.RichTextEditorLinkAddress], _address,
            address => _address = address, isAddress: true);
        if (Link is not null)
        {
            builder.OpenComponent<TbLink>(20);
            builder.AddComponentParameter(21, nameof(TbLink.Href), Link);
            builder.AddComponentParameter(22, nameof(TbLink.NewTab), true);
            builder.AddComponentParameter(23, nameof(TbLink.ChildContent),
                Text(Labels.OpenLink ?? Localizer[TbStrings.RichTextEditorOpenLink]));
            builder.CloseComponent();
        }

        builder.OpenComponent<TbDialogActions>(30);
        builder.AddComponentParameter(31, nameof(TbDialogActions.ChildContent), (RenderFragment)(actions =>
        {
            if (Link is not null)
            {
                DialogButtons.Add(actions, 0, this, Labels.RemoveLink ?? Localizer[TbStrings.RichTextEditorRemoveLink],
                    Variant.Outlined, Tone.Neutral, () => Dialog.CloseAsync(new(null, null)));
            }

            DialogButtons.Add(actions, 10, this, Localizer[TbStrings.DialogCancel], Variant.Outlined, Tone.Neutral,
                Dialog.CancelAsync);
            actions.OpenComponent<TbButton>(20);
            actions.AddComponentParameter(21, nameof(TbButton.Type), ButtonType.Submit);
            actions.AddComponentParameter(22, nameof(TbButton.ChildContent),
                Text(Labels.ApplyLink ?? Localizer[TbStrings.RichTextEditorApplyLink]));
            actions.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private void AddField(RenderTreeBuilder builder, int sequence, string label, string? value, Action<string> changed,
        bool isAddress = false)
    {
        builder.OpenComponent<TbTextField<string>>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbTextField<string>.Label), label);
        builder.AddComponentParameter(sequence + 2, nameof(TbTextField<string>.Value), value);
        builder.AddComponentParameter(sequence + 3, nameof(TbTextField<string>.ValueChanged),
            EventCallback.Factory.Create(this, changed));
        builder.AddComponentParameter(sequence + 4, nameof(TbTextField<string>.UpdateOnInput), true);
        if (isAddress)
        {
            builder.AddComponentParameter(sequence + 5, nameof(TbTextField<string>.Required), true);
            builder.AddComponentParameter(sequence + 6, nameof(TbTextField<string>.AutoFocus), true);
            builder.AddComponentParameter(sequence + 7, nameof(TbTextField<string>.Validation),
                (Func<string?, IEnumerable<string>>)Check);
            builder.AddComponentParameter(sequence + 8, nameof(TbTextField<string>.InputAttributes), AddressAttributes);
        }

        builder.CloseComponent();
    }

    private static RenderFragment Text(string text) => content => content.AddContent(0, text);

    private IEnumerable<string> Check(string? address)
    {
        var link = Complete(address);
        if (link.Length > 0 && !Schema.AllowsLink(link))
        {
            var schemes = Schema.LinkSchemes.Where(scheme => !RichTextSchema.RunsScript(scheme))
                .Select(scheme => scheme + ":").ToList();
            yield return schemes.Count == 0
                ? Labels.RelativeLinkOnly ?? Localizer[TbStrings.RichTextEditorRelativeLinkOnly]
                : string.Format(CultureInfo.CurrentCulture,
                    Labels.LinkNotAllowed ?? Localizer[TbStrings.RichTextEditorLinkNotAllowed], string.Join(", ", schemes));
        }
    }

    [GeneratedRegex(@"^[\w-]+(\.[\w-]+)+(:\d+)?([/?#]|$)")]
    private static partial Regex HostName();

    [GeneratedRegex(@"^[^@\s/:]+@[^@\s/]+\.[^@\s/]+$")]
    private static partial Regex EmailAddress();
}
