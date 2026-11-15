using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// A field's character counter: text-count.js keeps both texts up with typing, from the culture and templates given here.
internal static class FieldCount
{
    // Said is what screen readers hear for a count, with {0} left in for the count. The key must change with the text:
    // Blazor skips text it rendered unchanged, but typing changed it.
    public static RenderFragment Render(string id, int count, int? limit, object key, Func<int, string> said) => builder =>
    {
        var culture = CultureInfo.CurrentCulture;
        builder.OpenElement(0, "div");
        builder.SetKey(key);
        builder.AddAttribute(1, "id", id);
        builder.AddAttribute(2, "data-part", "count");
        builder.AddAttribute(3, "class", "tb-field__count");
        builder.AddAttribute(4, "data-culture", culture.Name);
        builder.AddAttribute(5, "data-limit", limit);
        builder.AddAttribute(6, "data-said", said(2));
        builder.AddAttribute(7, "data-said-one", limit is null ? said(1) : null);
        builder.OpenElement(9, "span");
        builder.AddAttribute(10, "data-part", "count-shown");
        builder.AddAttribute(11, "aria-hidden", "true");
        builder.AddContent(12, limit is { } max
            ? $"{count.ToString("N0", culture)} / {max.ToString("N0", culture)}" : count.ToString("N0", culture));
        builder.CloseElement();
        builder.OpenElement(13, "span");
        builder.AddAttribute(14, "data-part", "count-said");
        builder.AddAttribute(15, "class", "tb-visually-hidden");
        builder.AddContent(16, said(count).Replace("{0}", count.ToString("N0", culture), StringComparison.Ordinal));
        builder.CloseElement();
        builder.CloseElement();
    };

    // What screen readers hear for a count, with {0} left in for the count. With a limit, the plural form follows the
    // limit. The labels are the field's own words, or null for the app's text.
    public static string Said(ITbLocalizer localizer, int? limit, int count, string? countLabel, string? countOfLimitLabel)
    {
        var culture = CultureInfo.CurrentCulture;
        return limit is { } max
            ? string.Format(culture, countOfLimitLabel ?? localizer.Plural(TbStrings.TextFieldCountOfLimit, max), "{0}",
                max.ToString("N0", culture))
            : string.Format(culture, countLabel ?? localizer.Plural(TbStrings.TextFieldCount, count), "{0}");
    }

    // What's announced as the text nears the limit; the label is the field's own words, or null for the app's text.
    public static string CharactersLeft(ITbLocalizer localizer, int left, string? label) =>
        string.Format(CultureInfo.CurrentCulture, label ?? localizer.Plural(TbStrings.TextFieldCharactersLeft, left), left);
}
