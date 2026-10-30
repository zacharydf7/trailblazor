namespace Trailblazor;

// What a TbForm asks of the fields inside it, whatever their value type.
internal interface IFormField
{
    // Whether the field would fail validation now, without showing any message.
    bool HasProblems();

    void HideMessages();
}
