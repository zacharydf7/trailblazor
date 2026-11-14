using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbDateTimePicker</c>, found by its label.</summary>
public sealed class TbDateTimePickerLocator(ILocator scope, string label) : TbFieldLocator
{
    private protected override ILocator Control => Group;

    /// <summary>The group that holds the date box, the calendar button and the time box.</summary>
    public ILocator Group => scope.GetByRole(AriaRole.Group, new() { NameRegex = FieldLabel.Matching(label) });

    /// <summary>The box the date is typed in.</summary>
    public ILocator DateInput => Group.Locator("[data-part=date-input]");

    /// <summary>The browser's own time box.</summary>
    public ILocator TimeInput => Group.Locator("[data-part=time-input]");

    /// <summary>Types the date in the field's own format, fills the time and waits until the field has taken both; null
    /// clears the date. A statically rendered field gets the value in the browser's own date and time box.</summary>
    public async Task ChooseAsync(DateTime? value)
    {
        var staticBox = scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator("input[data-static]"));
        if (await staticBox.CountAsync() > 0)
        {
            await staticBox.FillAsync(
                value?.ToString($"yyyy-MM-ddT{TimeFormat(value.Value)}", CultureInfo.InvariantCulture) ?? "");
            return;
        }

        // A prerendered field is replaced once the page is interactive, so the typing waits for the boxes that read it.
        await Assertions.Expect(Group).ToHaveAttributeAsync("data-format", new Regex(".+"));
        var format = await Group.GetAttributeAsync("data-format");
        var culture = GregorianCulture.Named(await Group.GetAttributeAsync("data-culture"));
        await DateInput.FillAsync(value?.ToString(format, culture) ?? "");
        await DateInput.PressAsync("Tab");
        if (value is not { } chosen)
        {
            await Assertions.Expect(Group).ToHaveAttributeAsync("data-value", "");
            return;
        }

        await TimeInput.FillAsync(chosen.ToString(TimeFormat(chosen), CultureInfo.InvariantCulture));
        await TimeInput.PressAsync("Tab");
        await Assertions.Expect(Group).ToHaveAttributeAsync("data-value",
            new Regex($"^{chosen.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)}"));
    }

    /// <summary>The date and time the field holds, or null when it has none. For a statically rendered field, this
    /// reads the browser's own date and time box.</summary>
    public async Task<DateTime?> ValueAsync()
    {
        var staticBox = scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator("input[data-static]"));
        var text = await staticBox.CountAsync() > 0 ? await staticBox.InputValueAsync() : null;
        if (text is null)
        {
            // A prerendered field has no value of its own to report until the page is interactive.
            await Assertions.Expect(Group).ToHaveAttributeAsync("data-format", new Regex(".+"));
            text = await Group.GetAttributeAsync("data-value")
                ?? throw new InvalidOperationException($"The date-time picker \"{label}\" doesn't report its value.");
        }

        return string.IsNullOrEmpty(text) ? null
            : DateTime.ParseExact(text, ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"], CultureInfo.InvariantCulture);
    }

    // The time boxes take seconds only when the step has them, so whole minutes are written without.
    private static string TimeFormat(DateTime value) => value.Second == 0 ? "HH:mm" : "HH:mm:ss";
}
