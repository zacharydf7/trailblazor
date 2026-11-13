namespace Trailblazor;

// A tree grid row's place: its depth from 1, its position among the siblings that show, and whether its children
// show, null when it has none. ParentIndex is its parent's line, needed only in a virtualized grid; elsewhere it's -1,
// as for a top row, so a row doesn't redraw as the lines above it open and close.
internal readonly record struct TreePlace(int Level, int Position, int SetSize, bool? Open, int ParentIndex);
