namespace Trailblazor;

/// <summary>What a button does inside a form, the same as the HTML <c>type</c> attribute.</summary>
public enum ButtonType
{
    Button,
    Submit,

    /// <summary>Inside a <see cref="TbForm"/>, takes the fields and what they're bound to back to the values the parent
    /// last gave them, and hides their messages. See <see cref="TbForm.ResetAsync"/>.</summary>
    Reset,
}
