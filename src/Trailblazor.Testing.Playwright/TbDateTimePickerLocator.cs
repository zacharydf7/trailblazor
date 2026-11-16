using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbDateTimePicker</c>, found by its label or a locator.</summary>
public sealed class TbDateTimePickerLocator(ILocator scope, ILocator field) : TbFieldLocator
{
    private protected override ILocator Control => Group;

    /// <summary>The group that holds the date box, the calendar button and the time box.</summary>
    // An id goes on the field around the group, so the group is also looked for inside the match.
    public ILocator Group => @field
        .Locator("xpath=descendant-or-self::*[contains(concat(' ', normalize-space(@class), ' '), "
            + "' tb-date-time__boxes ')][1]")
        .And(scope.Locator(".tb-date-time__boxes"));

    // A statically rendered field is the browser's own date and time box, found the same way.
    private ILocator StaticBox => @field.Locator("xpath=descendant-or-self::input[@data-static][1]")
        .And(scope.Locator("input[data-static]"));

    /// <summary>The box the date is typed in.</summary>
    public ILocator DateInput => Group.Locator("[data-part=date-input]");

    /// <summary>The browser's own time box.</summary>
    public ILocator TimeInput => Group.Locator("[data-part=time-input]");

    /// <summary>Types the date in the field's own format, fills the time and waits until the field has taken both; null
    /// clears the date. A statically rendered field gets the value in the browser's own date and time box.</summary>
    public async Task ChooseAsync(DateTime? value)
    {
        var staticBox = StaticBox;
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
        // Each box commits on its own, and text a box already holds raises no change for the app to answer.
        var date = value?.ToString(format, culture) ?? "";
        await Commit.WaitAsync(DateInput, async () =>
        {
            var changes = await DateInput.InputValueAsync() != date;
            await DateInput.FillAsync(date);
            await DateInput.PressAsync("Tab");
            return changes;
        });
        if (value is not { } chosen)
        {
            await Assertions.Expect(Group).ToHaveAttributeAsync("data-value", "");
            return;
        }

        await Commit.WaitAsync(TimeInput, async () =>
        {
            // Filling a time box always raises a change, even with the time it already shows, since that text may hide the
            // value's seconds.
            await TimeInput.FillAsync(chosen.ToString(TimeFormat(chosen), CultureInfo.InvariantCulture));
            await TimeInput.PressAsync("Tab");
            return true;
        });
        await Assertions.Expect(Group).ToHaveAttributeAsync("data-value",
            new Regex($"^{chosen.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)}"));
    }

    /// <summary>Types this text in the date box as it is, leaves the box and waits until the field has taken it, so a
    /// test can enter text the field can't read and see its error; the time is left unchanged. A statically rendered
    /// field throws, and so does a read-only or disabled one.</summary>
    public async Task EnterAsync(string text)
    {
        var staticBox = StaticBox;
        await Dates.EnterAsync(await staticBox.CountAsync() > 0 ? staticBox : DateInput, text,
            () => Assertions.Expect(Group).ToHaveAttributeAsync("data-format", new Regex(".+")));
    }

    /// <summary>The date and time the field holds, or null when it has none. For a statically rendered field, this
    /// reads the browser's own date and time box.</summary>
    public async Task<DateTime?> ValueAsync()
    {
        var staticBox = StaticBox;
        var text = await staticBox.CountAsync() > 0 ? await staticBox.InputValueAsync() : null;
        if (text is null)
        {
            // A prerendered field has no value of its own to report until the page is interactive.
            await Assertions.Expect(Group).ToHaveAttributeAsync("data-format", new Regex(".+"));
            text = await Group.GetAttributeAsync("data-value")
                ?? throw new InvalidOperationException("This date-time picker doesn't report its value.");
        }

        return string.IsNullOrEmpty(text) ? null
            : DateTime.ParseExact(text, ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"], CultureInfo.InvariantCulture);
    }

    // The time boxes take seconds only when the step has them, so whole minutes are written without.
    private static string TimeFormat(DateTime value) => value.Second == 0 ? "HH:mm" : "HH:mm:ss";
}
