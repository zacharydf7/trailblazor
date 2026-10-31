namespace Trailblazor;

/// <summary>Tags an app adds to everything Trailblazor's components report, such as the customer the user works for, so
/// usage can be read per customer. Register one or more, scoped when the tags depend on the user, before or after
/// <c>AddTrailblazor</c>.</summary>
public interface ITelemetryTags
{
    /// <summary>The tags, read each time a component reports. Dashboards group by them, so keep each tag to a small
    /// set of values, and never put in anything personal.</summary>
    IEnumerable<KeyValuePair<string, object?>> Tags { get; }
}
