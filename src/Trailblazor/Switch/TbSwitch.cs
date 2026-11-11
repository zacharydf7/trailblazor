namespace Trailblazor;

/// <summary>An on/off switch for a setting that takes effect at once. It is a checkbox that screen readers announce as
/// a switch.</summary>
public sealed class TbSwitch : TbCheckbox<bool>
{
    private protected override string Block => "tb-switch";

    private protected override string? Role => "switch";
}
