namespace Trailblazor.Docs.Examples;

/// <summary>One task on the Plain bUnit and Playwright guide: the TestingComparison member that does it plainly in
/// Playwright and, when bUnit has one, in bUnit; whether the helpers do it; and what they add.</summary>
public sealed record PlainAndHelpersTask(string Id, string Title, string Member, bool InBunit, bool Helpers,
    string Short, string Adds)
{
    private const string File = "TestingComparison.cs";

    /// <summary>The plain bUnit code, or null when bUnit has nothing to do for the task.</summary>
    public string? PlainBunit => InBunit ? RecipeSnippet.Of(File, "PlainBunit", Member) : null;

    /// <summary>The plain Playwright code.</summary>
    public string PlainPlaywright => RecipeSnippet.Of(File, "PlainPlaywright", Member);

    /// <summary>The helpers' code, bUnit's first when it has one, or null when no helper does the task.</summary>
    public string? WithHelpers => !Helpers ? null : !InBunit
        ? $"// Playwright\n{RecipeSnippet.Of(File, "PlaywrightHelpers", Member)}"
        : $"// bUnit\n{RecipeSnippet.Of(File, "BunitHelpers", Member)}\n\n"
            + $"// Playwright\n{RecipeSnippet.Of(File, "PlaywrightHelpers", Member)}";
}

// The tasks the Plain bUnit and Playwright guide shows, in order. Search reads them here, since the page builds a
// section for each in a loop.
public static class PlainAndHelpersCatalog
{
    public static IReadOnlyList<PlainAndHelpersTask> All { get; } =
    [
        new("click-a-button", "Click a button", "ClickAButton", true, true,
            "Found by name; refuses a disabled or static button and waits out a busy one; waits for the app's handler",
            "The helpers find the button by its name, as a user does, and their error names the button when it's "
                + "missing or there's more than one. They refuse a disabled button, and one on a static page that "
                + "would do nothing; bUnit refuses a busy button too, and a browser waits until it's no longer busy. "
                + "In a browser, `ClickAsync()` waits until the page is interactive, then until the app has run "
                + "`OnClick`, or, for a handler that awaits a dialog's answer, until the dialog opens, so the test "
                + "needn't know what the handler changes. For a form's submit button it also waits until the form "
                + "has handled the submit."),
        new("type-and-leave", "Type into a field, then leave it", "TypeAndLeave", true, true,
            "The events the field commits on, in order; the app's answer; a re-keyed field",
            "`EnterAsync(text)` finds the field by its label, even with a required mark, gives it the events it "
                + "commits on, and in a browser waits until the app has the value, even when the app re-keys the "
                + "field and replaces it."),
        new("read-an-error", "Read a field's error", "ReadAnError", true, true,
            "The rendered messages and `aria-invalid`, not component state",
            "`Error`, `Errors` and `Invalid` read what users and screen readers get rather than the component's own "
                + "state: the messages the field shows, without its helper text, and `aria-invalid`."),
        new("choose-an-option", "Choose an option", "ChooseAnOption", true, true,
            "Found by name in the list the field controls; in a browser, waits for the select's script and the app",
            "`ChooseAsync(option)` finds the option by its name in the list the field controls. In a browser it first "
                + "waits until the select's script listens and opens the list, then waits until the app has the "
                + "value."),
        new("answer-a-dialog", "Answer a dialog", "AnswerADialog", true, true,
            "Waits until the dialog has left the page and the app has the answer",
            "`Dialog(title)` finds the dialog by its title wherever it renders, `ChooseAsync(button)` clicks a button "
                + "by its name, and in a browser waits until the dialog has faded out and left the page, which is when "
                + "the app gets the answer."),
        new("select-a-tree-row", "Expand and select tree rows", "SelectATreeRow", true, true,
            "Paths by text, opening each row along the way",
            "The tree's helpers follow rows by the texts along their path and open each row on the way. "
                + "`CheckAsync`, `ActivateAsync` and the drag steps take paths the same way, and `ExpandAllAsync()` "
                + "opens every row."),
        new("select-a-grid-row", "Read and select grid rows", "SelectAGridRow", true, true,
            "Rows as their cells' texts; selection by a row's text; a wait for a grid that loads",
            "`WaitForRowsAsync()` waits until a grid that loads its rows has them, `Rows` reads each row as its "
                + "cells' texts, and `SelectAsync(row)` selects a row by its text through its selection box."),
        new("read-a-tab-panel", "Switch tabs and read a panel", "ReadATabPanel", true, true,
            "Scoping to the tab's panel",
            "`SelectAsync(tab)` selects a tab by its name and waits for it. `PanelElement(tab)` is the panel itself, "
                + "and `Panel(tab)` scopes the other helpers to it, so a field every panel has can be told apart."),
        new("upload-a-file", "Upload a file", "UploadAFile", true, true,
            "Files by path or made in the test; refuses a field that can't take them; waits until the field has them",
            "`ChooseAsync` picks files by path or as files made in the test. It refuses a disabled, read-only or "
                + "static field, and in a browser waits until the field's script listens, then until the field has "
                + "the files."),
        new("wait-until-interactive", "Wait until the page is interactive", "WaitUntilInteractive", false, true,
            "A mark the app doesn't have to render, and what it doesn't promise",
            "`InteractiveAsync()` waits for the mark `TbRoot` or a `TbInteractiveMark` renders, so the app needn't "
                + "render one of its own. The mark says the page's interactive part runs, not that each component's "
                + "script has attached; the other helpers wait for their own components. bUnit renders interactively "
                + "from the start, so it has nothing to wait for."),
        new("no-helper", "A control with no helper", "ReadAControlWithNoHelper", true, false,
            "The role, name and `data-part` route to fall back on",
            "Find it by its role and accessible name, as here, or by the `data-part` hooks each component's page "
                + "lists. The `tb-*` classes are for styling and can change between versions."),
    ];
}
