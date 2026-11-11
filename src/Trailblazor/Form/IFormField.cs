namespace Trailblazor;

// What a TbForm asks of the fields inside it, whatever their value type.
internal interface IFormField
{
    // Whether the field would fail validation now, without showing any message.
    bool HasProblems();

    // Messages in the edit context that the field hides while it shows only its required message.
    IEnumerable<string> HiddenMessages();

    void HideMessages();

    // Goes back to the value the parent last gave it, showing no message, for a form reset.
    Task ResetAsync();

    // Counts the field in trailblazor.field.invalid when it has an error, after its form failed to submit.
    void CountIfInvalid();

    // Applies the form's new Disabled or ReadOnly.
    Task FollowFormAsync();
}
