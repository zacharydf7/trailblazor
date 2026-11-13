namespace Trailblazor;

// What a TbForm asks of the fields inside it, whatever their value type.
internal interface IFormField
{
    // Whether the field would fail validation now, without showing any message.
    bool HasProblems();

    // Whether the value differs from the one a reset goes back to, or the field holds text it can't read.
    bool IsDirty();

    // Makes the current value the one a reset goes back to.
    void MarkAsSaved();

    // The field's ValidationAsync check still running, if any.
    Task CheckingAsync();

    // Whether the field's ValidationAsync hasn't passed the value it holds now, such as when the check threw.
    bool AwaitsCheck();

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
}
