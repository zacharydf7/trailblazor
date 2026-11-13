namespace Trailblazor;

/// <summary>The words a <see cref="TbHotkeysHelp"/> shows and says to screen readers.</summary>
public sealed record HotkeysHelpLabels
{
    /// <summary>The dialog's title. Leave it null for the app's text for <see cref="TbStrings.HotkeysHelpTitle"/>.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>What the help's own hotkey does, as the help lists it. Leave it null for the app's text for
    /// <see cref="TbStrings.HotkeysHelpShow"/>.</summary>
    public string? Show { get; init; }

    /// <summary>The heading of the hotkeys that have no <c>Group</c>. Leave it null for the app's text for
    /// <see cref="TbStrings.HotkeysHelpGeneral"/>.</summary>
    public string? General { get; init; }

    /// <summary>The heading screen readers hear for the keys column. Leave it null for the app's text for
    /// <see cref="TbStrings.HotkeysHelpKeys"/>.</summary>
    public string? Keys { get; init; }

    /// <summary>The heading screen readers hear for the column that says what each hotkey does. Leave it null for the
    /// app's text for <see cref="TbStrings.HotkeysHelpAction"/>.</summary>
    public string? Action { get; init; }
}
