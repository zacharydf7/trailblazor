namespace Trailblazor;

/// <summary>App-wide dialog settings, set in <c>AddTrailblazor(options => ...)</c>. A dialog's own
/// <see cref="DialogOptions"/> override them.</summary>
public sealed class DialogSettings
{
    /// <summary>Whether Escape cancels a dialog whose options don't say. Defaults to true.</summary>
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>Whether a click outside a modal dialog cancels it when its options don't say. Defaults to true.
    /// </summary>
    public bool DismissOnBackdrop { get; set; } = true;

    /// <summary>Whether a dialog whose options don't say opens in the browser's top layer. Defaults to true; set it to
    /// false in an app whose dialogs host other libraries' popups. See <see cref="DialogOptions.TopLayer"/>.</summary>
    public bool TopLayer { get; set; } = true;

    /// <summary>CSS selectors for elements on the page's body that other libraries show popups in, such as a date
    /// picker's calendar created once as the page loads. A modal kept out of the top layer makes the rest of the page
    /// inert as it opens, apart from these and from popups added after it opens. Blazor's reconnect dialog and error
    /// bar are always left usable. An entry that isn't a valid selector stops such a dialog from opening, with an error
    /// quoting the selectors.</summary>
    public IList<string> OutsidePopups { get; } = [];
}
