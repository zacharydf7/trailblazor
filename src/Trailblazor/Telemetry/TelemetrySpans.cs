using System.Diagnostics;

namespace Trailblazor;

// Ends a span Telemetry.StartSpan started; safe on null, which StartSpan returns when it starts none.
internal static class TelemetrySpans
{
    // How the call went: succeeded, failed or canceled, and how many items came back. A failure records the exception's
    // type only, since its message may hold the app's data.
    public static void Finish(this Activity? span, string outcome, int? items = null, Exception? failure = null)
    {
        if (span is null)
        {
            return;
        }

        span.SetTag(Telemetry.OutcomeTag, outcome);
        if (items is not null)
        {
            span.SetTag(Telemetry.ItemsTag, items);
        }

        if (failure is not null)
        {
            span.SetTag("error.type", failure.GetType().FullName).SetStatus(ActivityStatusCode.Error);
        }

        span.Dispose();
    }
}
