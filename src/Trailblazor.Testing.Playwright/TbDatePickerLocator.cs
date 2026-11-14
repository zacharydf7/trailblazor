using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbDatePicker</c>, found by its label.</summary>
public sealed class TbDatePickerLocator(ILocator scope, string label) : TbFieldLocator
{
    private protected override ILocator Control => Input;

    /// <summary>The box the date is typed in.</summary>
    public ILocator Input => scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator("input"));

    /// <summary>Types the date in the field's own format and waits until the field has taken it; null clears it. A
    /// statically rendered field gets it in the browser's own date box.</summary>
    public async Task ChooseAsync(DateOnly? date)
    {
        if (await Input.GetAttributeAsync("data-static") is not null)
        {
            await Input.FillAsync(date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "");
            return;
        }

        // A prerendered field is replaced once the page is interactive, so the typing waits for the box that reads it.
        await Assertions.Expect(Input).ToHaveAttributeAsync("data-format", new Regex(".+"));
        var format = await Input.GetAttributeAsync("data-format");
        var culture = GregorianCulture.Named(await Input.GetAttributeAsync("data-culture"));
        var text = date?.ToString(format, culture) ?? "";
        await Input.FillAsync(text);
        await Input.PressAsync("Tab");
        await Assertions.Expect(Input).ToHaveAttributeAsync("data-value",
            date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "");
    }

    /// <summary>The date the field holds, or null when it has none. For a statically rendered field, this reads the
    /// browser's own date box.</summary>
    public async Task<DateOnly?> ValueAsync()
    {
        if (await Input.GetAttributeAsync("data-static") is not null)
        {
            return ReadIso(await Input.InputValueAsync());
        }

        // A prerendered field has no value of its own to report until the page is interactive.
        await Assertions.Expect(Input).ToHaveAttributeAsync("data-format", new Regex(".+"));
        return ReadIso(await Input.GetAttributeAsync("data-value")
            ?? throw new InvalidOperationException($"The date picker \"{label}\" doesn't report its date."));
    }

    internal static DateOnly? ReadIso(string? text) => string.IsNullOrEmpty(text) ? null
        : DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
