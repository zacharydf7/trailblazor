namespace Trailblazor.Spikes.Client.Charts;

// One series of the spike chart: a value for each of the chart's places. Stepped holds each value until the next.
public sealed record SpikeSeries(string Name, List<double> Values, bool Dashed = false, bool Stepped = false);
