namespace Trailblazor;

// A box inside the dock, in percent of its width and height. X counts from the side where text starts.
internal readonly record struct DockBox(double X, double Y, double Width, double Height);

// A group that shows a panel: its box, the panels it holds that the dock can show, and the one showing.
internal sealed record DockGroupBox(DockGroup Group, DockBox Box, IReadOnlyList<string> Panels, string Active);

// The separator between two parts of a split that both show a panel, with the parts' places in the split and their
// boxes.
internal sealed record DockSeparatorBox(DockSplit Split, int Before, int After, DockBox BeforeBox, DockBox AfterBox);

// What a dock shows, in reading order. Separator n sits between group n and group n + 1, since every part of a split
// begins and ends with a group.
internal sealed record DockArrangement(IReadOnlyList<DockGroupBox> Groups, IReadOnlyList<DockSeparatorBox> Separators);
