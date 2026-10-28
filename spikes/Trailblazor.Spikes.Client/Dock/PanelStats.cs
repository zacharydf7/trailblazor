namespace Trailblazor.Spikes.Client.Dock;

public sealed class PanelCounts
{
    public int Initializations { get; set; }

    public int Renders { get; set; }

    public int Disposals { get; set; }
}

// Lifecycle counts per panel, read by the measurements to prove content was never re-created or re-rendered.
public sealed class PanelStats
{
    public Dictionary<string, PanelCounts> Panels { get; } = [];

    public PanelCounts For(string panelId)
    {
        if (!Panels.TryGetValue(panelId, out var counts))
        {
            counts = new PanelCounts();
            Panels[panelId] = counts;
        }

        return counts;
    }
}
