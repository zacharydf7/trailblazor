namespace Trailblazor;

/// <summary>The shape of the content a <see cref="TbBadge"/> sits on, which decides where its corner is.</summary>
public enum BadgeOverlap
{
    /// <summary>Square or rectangular content, such as a button; the badge sits on its corner.</summary>
    Rectangular,

    /// <summary>Round content, such as an avatar; the badge sits on the circle's edge, toward the corner.</summary>
    Circular,
}
