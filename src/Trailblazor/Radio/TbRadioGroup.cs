using System.Diagnostics.CodeAnalysis;

namespace Trailblazor;

/// <summary>Picks one value from a short list of <see cref="TbRadio{TValue}"/> options shown all at once.</summary>
public partial class TbRadioGroup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>
    : IRadioGroup;
