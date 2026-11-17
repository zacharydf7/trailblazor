using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>One step of a <see cref="TbTour"/>: the part of the page it points at and what it says about it.</summary>
/// <param name="Target">A CSS selector for the element the step points at, such as <c>"#new-trail"</c>. A step whose
/// target isn't on the page, or whose selector isn't valid, shows in the middle of the window.</param>
/// <param name="Title">The step's heading, which also names it for screen readers.</param>
public sealed record TourStep(string Target, string Title)
{
    /// <summary>The step's text, under its title.</summary>
    public string? Body { get; init; }

    /// <summary>Richer content shown in place of <see cref="Body"/>.</summary>
    public RenderFragment? Content { get; init; }

    /// <summary>Which side of the target the step shows on. Defaults to <see cref="Placement.Bottom"/>; it moves to
    /// the other side when there's no room.</summary>
    public Placement Placement { get; init; } = Placement.Bottom;

    /// <summary>How the step lines up with the target along that side. Defaults to
    /// <see cref="PlacementAlign.Start"/>.</summary>
    public PlacementAlign Align { get; init; }
}
