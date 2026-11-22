namespace Trailblazor;

/// <summary>A command of the app's own among a <see cref="TbRichTextEditor"/>'s slash commands, such as one that puts
/// in a signature or asks the app for a template.</summary>
/// <param name="text">The command's name in the list. What is typed after the slash is matched against it.</param>
public sealed class RichTextCommand(string text)
{
    // Prefixes the list id of each of the app's commands, which ends with the command's index in Commands.
    internal const string IdPrefix = "app:";

    /// <summary>The command's name in the list.</summary>
    public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));

    /// <summary>A line under the name that says what the command does.</summary>
    public string? Description { get; init; }

    /// <summary>More words the command is found by, such as "sign off" for a signature.</summary>
    public string? Keywords { get; init; }

    /// <summary>HTML the command puts in where the slash was: text joins the line, and a heading, quote, list or
    /// block of the app's own starts under it. Only what the editor's formats and blocks allow goes in, as for
    /// pasted text, but <see cref="TbRichTextEditor.TransformPaste"/> doesn't run for it.</summary>
    public string? Html { get; init; }

    /// <summary>Called when the command is chosen, after its <see cref="Html"/> has gone in. It runs outside the
    /// page's own events, so call <c>StateHasChanged</c> if it changes what the page shows.</summary>
    public Func<Task>? OnChosen { get; init; }
}
