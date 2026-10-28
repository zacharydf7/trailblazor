namespace Trailblazor.Spikes.Client.Grid;

// Render and request counts, read by the measurements.
public sealed class GridStats
{
    private readonly HashSet<int> _activeRequests = [];
    private int _nextRequestId;

    public int RowInstances { get; set; }

    public int RowRenders { get; set; }

    public int RequestsStarted { get; private set; }

    public int RequestsCompleted { get; private set; }

    public int RequestsCancelled { get; private set; }

    // Requests that started and were neither cancelled nor completed, at the busiest moment.
    public int MaxActiveRequests { get; private set; }

    public int StartRequest()
    {
        var id = _nextRequestId++;
        RequestsStarted++;
        _activeRequests.Add(id);
        MaxActiveRequests = Math.Max(MaxActiveRequests, _activeRequests.Count);
        return id;
    }

    public void CancelRequest(int id)
    {
        if (_activeRequests.Remove(id))
        {
            RequestsCancelled++;
        }
    }

    public void CompleteRequest(int id)
    {
        if (_activeRequests.Remove(id))
        {
            RequestsCompleted++;
        }
    }
}
