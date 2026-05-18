using System.Net;
using System.Text;

namespace WifiProvisioning.Tests.TestHelpers;

/// <summary>
/// A test double for HttpMessageHandler that returns canned responses
/// or throws exceptions, and records the requests it saw for assertions.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string> RequestBodies { get; } = new();

    public FakeHttpMessageHandler(HttpStatusCode status, string? jsonBody = null)
        : this((_, _) =>
        {
            var resp = new HttpResponseMessage(status);
            if (jsonBody is not null)
            {
                resp.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }
            return Task.FromResult(resp);
        })
    {
    }

    public FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    public static FakeHttpMessageHandler Throws(Exception ex) =>
        new((_, _) => throw ex);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (request.Content is not null)
        {
            RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        }
        else
        {
            RequestBodies.Add(string.Empty);
        }
        return await _handler(request, cancellationToken).ConfigureAwait(false);
    }
}