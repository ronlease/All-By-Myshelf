using System.Net;

namespace AllByMyshelf.Unit.TestDoubles;

/// <summary>Captures the last request and returns a fixed response.</summary>
internal sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(response);
    }
}

/// <summary>Captures all requests and returns responses from a queue in order.</summary>
internal sealed class CapturingQueuedHandler(Queue<HttpResponseMessage> responses)
    : HttpMessageHandler
{
    public List<HttpRequestMessage> CapturedRequests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CapturedRequests.Add(request);
        return Task.FromResult(responses.Dequeue());
    }
}

/// <summary>Returns responses from a queue in order.</summary>
internal sealed class QueuedResponseHandler(Queue<HttpResponseMessage> responses)
    : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(responses.Dequeue());
    }
}

/// <summary>Always returns the same pre-built response.</summary>
internal sealed class StaticResponseHandler(HttpStatusCode statusCode, HttpContent content)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(statusCode) { Content = content });
}
