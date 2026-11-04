using System.Net;

namespace Trailblazor.Tests.Docs;

// Answers the docs site's fetches from files a test sets, by their path, and with 404 for any other.
public sealed class FakeSite : HttpMessageHandler
{
    public Dictionary<string, string> Files { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(Files.TryGetValue(request.RequestUri!.AbsolutePath.TrimStart('/'), out var content)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
}
