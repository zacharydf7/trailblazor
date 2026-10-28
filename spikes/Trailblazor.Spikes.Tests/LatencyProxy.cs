using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Trailblazor.Spikes.Tests;

// A TCP relay that delays every chunk a fixed time each way; queued delays overlap, as on a real link.
public sealed class LatencyProxy : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Uri _target;
    private readonly TimeSpan _oneWayDelay;
    private readonly Task _acceptLoop;

    public LatencyProxy(Uri target, TimeSpan oneWayDelay)
    {
        _target = target;
        _oneWayDelay = oneWayDelay;
        _listener.Start();
        _acceptLoop = AcceptLoopAsync();
    }

    public Uri Address => new($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        await _acceptLoop;
    }

    private async Task AcceptLoopAsync()
    {
        var relays = new List<Task>();
        try
        {
            while (true)
            {
                relays.Add(RelayAsync(await _listener.AcceptTcpClientAsync(_stop.Token)));
            }
        }
        catch (OperationCanceledException)
        {
        }

        await Task.WhenAll(relays);
    }

    private async Task RelayAsync(TcpClient client)
    {
        using var browserSide = client;
        using var hostSide = new TcpClient();
        await hostSide.ConnectAsync(_target.Host, _target.Port, _stop.Token);
        await Task.WhenAny(PumpAsync(browserSide.GetStream(), hostSide.GetStream()),
            PumpAsync(hostSide.GetStream(), browserSide.GetStream()));
    }

    private async Task PumpAsync(Stream from, Stream to)
    {
        var queue = Channel.CreateUnbounded<(byte[] Data, DateTime Due)>();
        var writer = WriteWhenDueAsync(queue.Reader, to);
        var buffer = new byte[16 * 1024];
        try
        {
            int read;
            while ((read = await from.ReadAsync(buffer, _stop.Token)) > 0)
            {
                queue.Writer.TryWrite((buffer[..read], DateTime.UtcNow + _oneWayDelay));
            }
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException)
        {
            // The browser or the host closed the connection; that ends this direction of the relay.
        }

        queue.Writer.TryComplete();
        await writer;
    }

    private async Task WriteWhenDueAsync(ChannelReader<(byte[] Data, DateTime Due)> reader, Stream to)
    {
        try
        {
            await foreach (var (data, due) in reader.ReadAllAsync())
            {
                // Task.Delay can overshoot by a Windows timer tick (about 15.6 ms), so the last stretch spins.
                var wait = due - DateTime.UtcNow;
                if (wait > TimeSpan.FromMilliseconds(20))
                {
                    await Task.Delay(wait - TimeSpan.FromMilliseconds(16), _stop.Token);
                }

                while (DateTime.UtcNow < due)
                {
                    Thread.SpinWait(100);
                }

                await to.WriteAsync(data, _stop.Token);
            }
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException)
        {
            // The other side closed the connection; nothing is left to deliver.
        }
    }
}
