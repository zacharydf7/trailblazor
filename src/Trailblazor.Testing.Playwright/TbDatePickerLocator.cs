using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbDatePicker</c>, found by its label.</summary>
public sealed class TbDatePickerLocator(IPage page, string label)
{
    /// <summary>The box the date is typed in.</summary>
    public ILocator Input => page.GetByLabel(FieldLabel.Matching(label)).And(page.Locator("input"));

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
        var culture = CultureInfo.GetCultureInfo(await Input.GetAttributeAsync("data-culture") ?? "");
        var text = date?.ToString(format, culture) ?? "";
        await Input.FillAsync(text);
        await Input.PressAsync("Tab");
        await Assertions.Expect(Input).ToHaveAttributeAsync("data-value",
            date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "");
    }
}
