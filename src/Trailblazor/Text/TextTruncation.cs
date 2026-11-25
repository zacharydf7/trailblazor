namespace Trailblazor;

/// <summary>Where <see cref="TbText"/> cuts text that doesn't fit.</summary>
public enum TextTruncation
{
    /// <summary>At the end, with an ellipsis after what's kept, which suits sentences and names.</summary>
    End,

    /// <summary>In the middle, keeping the start and the end, which suits file names, IDs and paths, such as
    /// "report_…_v3.pdf".</summary>
    Middle,
}
