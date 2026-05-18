using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WifiProvisioning.Core.Exceptions;
using WifiProvisioning.Core.Services;
using WifiProvisioning.Tests.TestHelpers;
using Xunit;
using static WifiProvisioning.Core.Exceptions.ProvisioningException;

namespace WifiProvisioning.Tests.Services;

public sealed class SpeedProfileClientTests
{
    private static SpeedProfileClient Build(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://stub") },
            NullLogger<SpeedProfileClient>.Instance);

    [Fact]
    public async Task GetSpeedProfileAsync_ReturnsMatchingProfile_OnSuccess()
    {
        var json = """
        {
          "requestId": "REQ-1",
          "speedProfiles": [
            {"code": "S100","downloadSpeedMbps": 100,"uploadSpeedMbps": 20},
            {"code": "S500","downloadSpeedMbps": 500,"uploadSpeedMbps": 100},
            {"code": "S123","downloadSpeedMbps": 250,"uploadSpeedMbps": 50}
          ]
        }
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var sut = Build(handler);

        var result = await sut.GetSpeedProfileAsync("S123", CancellationToken.None);

        result.Code.Should().Be("S123");
        result.DownloadSpeedMbps.Should().Be(250);
        result.UploadSpeedMbps.Should().Be(50);

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/speed-profiles");
    }

    [Fact]
    public async Task GetSpeedProfileAsync_ThrowsSpeedNotAvailable_WhenNoProfileMatches()
    {
        var json = """
        {"requestId":"REQ-1","speedProfiles":[{"code":"S100","downloadSpeedMbps":100,"uploadSpeedMbps":20}]}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var sut = Build(handler);

        var act = async () => await sut.GetSpeedProfileAsync("S999", CancellationToken.None);

        await act.Should().ThrowAsync<SpeedNotAvailableException>()
            .WithMessage("*S999*");
    }

    [Fact]
    public async Task GetSpeedProfileAsync_ThrowsSpeedNotAvailable_WhenListIsEmpty()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"requestId":"REQ-1","speedProfiles":[]}""");
        var sut = Build(handler);

        var act = async () => await sut.GetSpeedProfileAsync("S100", CancellationToken.None);

        await act.Should().ThrowAsync<SpeedNotAvailableException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task GetSpeedProfileAsync_ThrowsServiceException_OnNonSuccess(HttpStatusCode status)
    {
        var handler = new FakeHttpMessageHandler(status, "fail");
        var sut = Build(handler);

        var act = async () => await sut.GetSpeedProfileAsync("S123", CancellationToken.None);

        await act.Should().ThrowAsync<SpeedProfileServiceException>()
            .WithMessage($"*{(int)status}*");
    }

    [Fact]
    public async Task GetSpeedProfileAsync_ThrowsServiceException_OnInvalidJson()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "not-json-at-all");
        var sut = Build(handler);

        var act = async () => await sut.GetSpeedProfileAsync("S123", CancellationToken.None);

        await act.Should().ThrowAsync<SpeedProfileServiceException>()
            .WithMessage("*Invalid JSON*");
    }

    [Fact]
    public async Task GetSpeedProfileAsync_ThrowsServiceException_OnNullBody()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "null");
        var sut = Build(handler);

        var act = async () => await sut.GetSpeedProfileAsync("S123", CancellationToken.None);

        await act.Should().ThrowAsync<SpeedProfileServiceException>()
            .WithMessage("*null*");
    }

    [Fact]
    public async Task GetSpeedProfileAsync_ThrowsServiceException_OnTimeout()
    {
        var handler = FakeHttpMessageHandler.Throws(new TaskCanceledException("timeout"));
        var sut = Build(handler);

        var act = async () => await sut.GetSpeedProfileAsync("S123", CancellationToken.None);

        await act.Should().ThrowAsync<SpeedProfileServiceException>()
            .WithMessage("*timed out*");
    }

    [Fact]
    public async Task GetSpeedProfileAsync_ThrowsServiceException_OnNetworkError()
    {
        var handler = FakeHttpMessageHandler.Throws(new HttpRequestException("dns failed"));
        var sut = Build(handler);

        var act = async () => await sut.GetSpeedProfileAsync("S123", CancellationToken.None);

        await act.Should().ThrowAsync<SpeedProfileServiceException>()
            .WithMessage("*Network error*");
    }
}