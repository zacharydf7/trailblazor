using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbCheckboxGroup</c>, found by its label. Its options are named by their text.</summary>
public sealed class TbCheckboxGroupLocator(ILocator scope, string label)
{
    private ILocator Group => scope.GetByRole(AriaRole.Group, new() { NameRegex = FieldLabel.Matching(label) })
        .And(scope.Locator("fieldset.tb-checkbox-group"));

    /// <summary>The checked options' names, in order.</summary>
    public async Task<IReadOnlyList<string>> CheckedAsync() =>
        await Group.EvaluateAsync<string[]>(Choices.CheckedNames);

    /// <summary>Checks the options with these names, leaving the others as they are, and waits until the app has each.
    /// A disabled group or option throws.</summary>
    public Task CheckAsync(params string[] options) => SetAsync(options, on: true);

    /// <summary>Unchecks the options with these names, leaving the others as they are, and waits as
    /// <see cref="CheckAsync"/> does.</summary>
    public Task UncheckAsync(params string[] options) => SetAsync(options, on: false);

    private async Task SetAsync(string[] options, bool on)
    {
        ArgumentOutOfRangeException.ThrowIfZero(options.Length, nameof(options));
        foreach (var option in options)
        {
            await Ticks.SetAsync(Group.GetByRole(AriaRole.Checkbox, new() { Name = option, Exact = true }), on);
        }
    }
}
