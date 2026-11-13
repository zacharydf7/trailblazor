using System.ComponentModel;

namespace Trailblazor.Internal.Events;

/// <summary>A stroke drawn on a <see cref="TbSignaturePad"/>, as its points' coordinates in turn: x, then y. Raised by the
/// pad's script; consumers don't normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SignatureStrokeEventArgs : EventArgs
{
    /// <summary>Each point's x and y in the signature's space, one after the other.</summary>
    public double[] Points { get; set; } = [];
}
