using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using WifiProvisioning.Core.Exceptions;
using WifiProvisioning.Core.Models.Domain;
using WifiProvisioning.Core.Models.Input;
using WifiProvisioning.Core.Services;
using Xunit;
using static WifiProvisioning.Core.Exceptions.ProvisioningException;

namespace WifiProvisioning.Tests.Integration;

public sealed class ProvisioningApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IProvisioningService> _service = new();

    public ProvisioningApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SpeedProfileApi:BaseUrl"] = "http://localhost:0",
                    ["ActivationApi:BaseUrl"] = "http://localhost:0"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IProvisioningService));
                services.AddSingleton(_service.Object);
            });
        });
    }

    [Fact]
    public async Task Post_ReturnsOk_OnSuccess()
    {
        _service
            .Setup(s => s.ProvisionAsync(It.IsAny<ProvisioningOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProvisioningResult
            {
                OrderId = "123",
                ActivationId = "ACT-1",
                Status = "ACTIVE",
                ProfileId = "S123",
                DownloadSpeedMbps = 250,
                UploadSpeedMbps = 50,
                ActivatedAt = DateTimeOffset.UtcNow
            });

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/provisioning", BuildValidOrder());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProvisioningResult>();
        body!.ActivationId.Should().Be("ACT-1");
    }

    [Fact]
    public async Task Post_Returns400_OnInvalidRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/provisioning", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _service.Verify(
            s => s.ProvisionAsync(It.IsAny<ProvisioningOrderRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Post_Returns400_OnInvalidProvisioningOrder()
    {
        _service
            .Setup(s => s.ProvisionAsync(It.IsAny<ProvisioningOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidProvisioningOrderException("missing speed"));

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/provisioning", BuildValidOrder());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Post_Returns404_OnSpeedNotAvailable()
    {
        _service
            .Setup(s => s.ProvisionAsync(It.IsAny<ProvisioningOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SpeedNotAvailableException("no profile for S999"));

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/provisioning", BuildValidOrder());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Post_Returns502_OnSpeedProfileServiceFailure()
    {
        _service
            .Setup(s => s.ProvisionAsync(It.IsAny<ProvisioningOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SpeedProfileServiceException("upstream down"));

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/provisioning", BuildValidOrder());

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Post_Returns504_OnSpeedProfileTimeout()
    {
        _service
            .Setup(s => s.ProvisionAsync(It.IsAny<ProvisioningOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SpeedProfileServiceException(
                "Speed Profile API call timed out.", new TaskCanceledException()));

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/provisioning", BuildValidOrder());

        response.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
    }

    [Fact]
    public async Task Post_Returns502_OnActivationFailure()
    {
        _service
            .Setup(s => s.ProvisionAsync(It.IsAny<ProvisioningOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ActivationServiceException("activation down"));

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/provisioning", BuildValidOrder());

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Post_Returns500_OnUnexpectedException()
    {
        _service
            .Setup(s => s.ProvisionAsync(It.IsAny<ProvisioningOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("oops"));

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/provisioning", BuildValidOrder());

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private static ProvisioningOrderRequest BuildValidOrder() => new()
    {
        ExternalId = "123",
        Description = "Activate",
        OrderItem = new OrderItem
        {
            Id = "1",
            Service = new Service
            {
                Id = "",
                ServiceSpecification = new ServiceSpecification { Id = "1234", Name = "We" },
                ServiceCharacteristic = new List<ServiceCharacteristic>
                {
                    BuildChar(CharacteristicNames.CustomerId, "C1233"),
                    BuildChar(CharacteristicNames.CustomerName, "John"),
                    BuildChar(CharacteristicNames.CustomerAddress, "1333 J Boston USA"),
                    BuildChar(CharacteristicNames.SpeedProfile, "S123")
                }
            }
        }
    };

    private static ServiceCharacteristic BuildChar(string name, string value) => new()
    {
        Name = name,
        ValueType = "string",
        Value = new Dictionary<string, string>
        {
            ["@type"] = "string",
            [name] = value
        }
    };
}