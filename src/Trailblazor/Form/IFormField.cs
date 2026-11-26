namespace Trailblazor;

// What a TbForm asks of the fields inside it, whatever their value type.
internal interface IFormField
{
    // Whether the field would fail validation now, without showing any message.
    bool HasProblems();

    // The messages the field shows under it now.
    IReadOnlyList<string> ShownErrors();

    // The id of the element a link to the field points at: its control, or the element holding its controls.
    string ControlId { get; }

    // Moves focus to the field, as a link in a validation summary does.
    ValueTask FocusAsync(bool preventScroll = false);

    // Whether the value differs from the one a reset goes back to, or the field holds text it can't read.
    bool IsDirty();

    // Whether the field holds text it couldn't read, which leaves its last value in place.
    bool HasParseError { get; }

    // Makes the current value the one a reset goes back to.
    void MarkAsSaved();

    // The field's ValidationAsync check still running, if any.
    Task CheckingAsync();

    // Whether the field's ValidationAsync hasn't passed the value it holds now, such as when the check threw.
    bool AwaitsCheck();

    // Whether the field's ValidationAsync check is running for the value it holds and hasn't answered.
    bool IsChecking();

    // Messages in the edit context that the field hides while it shows only its required message.
    IEnumerable<string> HiddenMessages();

    void HideMessages();

    // Edit-context messages the field rewords, such as a model's default [Required] message.
    IEnumerable<(string From, string To)> RenamedMessages();

    // Goes back to the value the parent last gave it, showing no message, for a form reset.
    Task ResetAsync();

    // Counts the field in trailblazor.field.invalid when it has an error, after its form failed to submit.
    void CountIfInvalid();

    // Applies the form's new Disabled or ReadOnly.
    Task FollowFormAsync();

    // The field as smart paste describes it to the app's model, or null for one it can't fill.
    SmartPasteField? DescribeForPaste();

    // Sets the value from text the model read out of pasted text; false when the text isn't a value the field takes,
    // or names none of the options the field offered.
    Task<bool> PasteAsync(string text);
}
