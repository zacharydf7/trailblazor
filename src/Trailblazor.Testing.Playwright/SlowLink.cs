using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>Runs a page as if over a slow link to its interactive server app, so a test can check that the page keeps
/// up with fast keys and clicks.</summary>
public static class SlowLink
{
    /// <summary>Holds back every message this page sends to the app's circuit, such as a click, a key or a script's
    /// reply, by <paramref name="delay"/>, as a slow link would. Messages from the app arrive without delay. Call it
    /// before the page opens the app.</summary>
    public static Task UseSlowLinkAsync(this IPage page, TimeSpan delay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        return page.RouteWebSocketAsync(new Regex("/_blazor"), socket =>
        {
            var server = socket.ConnectToServer();
            // Each message goes out delay after the page sent it, in order, so delays overlap instead of adding up.
            var sending = Task.CompletedTask;
            socket.OnMessage(frame =>
            {
                var sent = Stopwatch.GetTimestamp();
                sending = sending.ContinueWith(async _ =>
                {
                    var left = delay - Stopwatch.GetElapsedTime(sent);
                    if (left > TimeSpan.Zero)
                    {
                        await Task.Delay(left);
                    }

                    if (frame.Binary is { } bytes)
                    {
                        server.Send(bytes);
                    }
                    else
                    {
                        server.Send(frame.Text ?? "");
                    }
                }, TaskScheduler.Default).Unwrap();
            });
        });
    }
}
