using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WifiProvisioning.Core.Exceptions;
using WifiProvisioning.Core.Models.Domain;
using WifiProvisioning.Core.Services;
using WifiProvisioning.Tests.TestHelpers;
using Xunit;
using static WifiProvisioning.Core.Exceptions.ProvisioningException;

namespace WifiProvisioning.Tests.Services;

public sealed class ActivationClientTests
{
    private static ActivationClient Build(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://stub") },
            NullLogger<ActivationClient>.Instance);

    private static ActivationRequest SampleRequest() => new()
    {
        CustomerId = "C1",
        CustomerAddress = "Amsterdam",
        DownstreamSpeed = "100",
        UpstreamSpeed = "20"
    };

    [Fact]
    public async Task ActivateAsync_ReturnsResponse_OnSuccess()
    {
        var json = """
        {"activationId":"ACT-1","status":"ACTIVE","activatedAt":"2026-05-17T10:00:00Z"}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var sut = Build(handler);

        var result = await sut.ActivateAsync(SampleRequest(), CancellationToken.None);

        result.ActivationId.Should().Be("ACT-1");
        result.Status.Should().Be("ACTIVE");

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/activation");
        handler.RequestBodies[0].Should().Contain("\"customerId\":\"C1\"");
        handler.RequestBodies[0].Should().Contain("\"downstreamSpeed\":\"100\"");
        handler.RequestBodies[0].Should().Contain("\"upstreamSpeed\":\"20\"");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ActivateAsync_ThrowsServiceException_OnNonSuccess(HttpStatusCode status)
    {
        var handler = new FakeHttpMessageHandler(status, "fail");
        var sut = Build(handler);

        var act = async () => await sut.ActivateAsync(SampleRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ActivationServiceException>()
            .WithMessage($"*{(int)status}*");
    }

    [Fact]
    public async Task ActivateAsync_ThrowsServiceException_OnInvalidJson()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{not json");
        var sut = Build(handler);

        var act = async () => await sut.ActivateAsync(SampleRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ActivationServiceException>()
            .WithMessage("*Invalid JSON*");
    }

    [Fact]
    public async Task ActivateAsync_ThrowsServiceException_OnNullBody()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "null");
        var sut = Build(handler);

        var act = async () => await sut.ActivateAsync(SampleRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ActivationServiceException>()
            .WithMessage("*null*");
    }

    [Fact]
    public async Task ActivateAsync_ThrowsServiceException_OnMissingActivationId()
    {
        var json = """{"activationId":"","status":"ACTIVE","activatedAt":"2026-01-01T00:00:00Z"}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var sut = Build(handler);

        var act = async () => await sut.ActivateAsync(SampleRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ActivationServiceException>()
            .WithMessage("*ActivationId*");
    }

    [Fact]
    public async Task ActivateAsync_ThrowsServiceException_OnTimeout()
    {
        var handler = FakeHttpMessageHandler.Throws(new TaskCanceledException("timeout"));
        var sut = Build(handler);

        var act = async () => await sut.ActivateAsync(SampleRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ActivationServiceException>()
            .WithMessage("*timed out*");
    }

    [Fact]
    public async Task ActivateAsync_ThrowsServiceException_OnNetworkError()
    {
        var handler = FakeHttpMessageHandler.Throws(new HttpRequestException("connection refused"));
        var sut = Build(handler);

        var act = async () => await sut.ActivateAsync(SampleRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ActivationServiceException>()
            .WithMessage("*Network error*");
    }

    [Fact]
    public async Task ActivateAsync_Throws_WhenRequestIsNull()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var sut = Build(handler);

        var act = async () => await sut.ActivateAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}