using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WifiProvisioning.Core.Exceptions;
using WifiProvisioning.Core.Mapping;
using WifiProvisioning.Core.Models.Domain;
using WifiProvisioning.Core.Models.Input;
using WifiProvisioning.Core.Services;
using Xunit;
using static WifiProvisioning.Core.Exceptions.ProvisioningException;

namespace WifiProvisioning.Tests.Services;

public sealed class ProvisioningServiceTests
{
    private readonly Mock<ISpeedProfileClient> _speedClient = new(MockBehavior.Strict);
    private readonly Mock<IActivationClient> _activationClient = new(MockBehavior.Strict);

    private ProvisioningService BuildSut() =>
        new(_speedClient.Object, _activationClient.Object,
            NullLogger<ProvisioningService>.Instance);

    [Fact]
    public async Task ProvisionAsync_HappyPath_ReturnsCombinedResult()
    {
        var order = BuildValidOrder();
        var profile = new SpeedProfile
        {
            Code = "S123",
            DownloadSpeedMbps = 250,
            UploadSpeedMbps = 50
        };
        var activatedAt = DateTimeOffset.UtcNow;
        var activation = new ActivationResponse
        {
            ActivationId = "ACT-99",
            Status = "ACTIVE",
            ActivatedAt = activatedAt
        };

        _speedClient
            .Setup(c => c.GetSpeedProfileAsync("S123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        ActivationRequest? capturedActivation = null;
        _activationClient
            .Setup(c => c.ActivateAsync(It.IsAny<ActivationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ActivationRequest, CancellationToken>((req, _) => capturedActivation = req)
            .ReturnsAsync(activation);

        var sut = BuildSut();

        var result = await sut.ProvisionAsync(order, CancellationToken.None);

        capturedActivation.Should().NotBeNull();
        capturedActivation!.CustomerId.Should().Be("C1233");
        capturedActivation.CustomerAddress.Should().Be("1333 Amsterdam");
        capturedActivation.DownstreamSpeed.Should().Be("250");
        capturedActivation.UpstreamSpeed.Should().Be("50");

        result.OrderId.Should().Be("123");
        result.ActivationId.Should().Be("ACT-99");
        result.Status.Should().Be("ACTIVE");
        result.ProfileId.Should().Be("S123");
        result.DownloadSpeedMbps.Should().Be(250);
        result.UploadSpeedMbps.Should().Be(50);
        result.ActivatedAt.Should().Be(activatedAt);

        _speedClient.VerifyAll();
        _activationClient.VerifyAll();
    }

    [Fact]
    public async Task ProvisionAsync_DoesNotCallActivation_WhenSpeedLookupFails()
    {
        _speedClient
            .Setup(c => c.GetSpeedProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SpeedNotAvailableException("nope"));

        var sut = BuildSut();

        var act = async () => await sut.ProvisionAsync(BuildValidOrder(), CancellationToken.None);

        await act.Should().ThrowAsync<SpeedNotAvailableException>();

        _activationClient.Verify(
            c => c.ActivateAsync(It.IsAny<ActivationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProvisionAsync_PropagatesActivationFailure()
    {
        _speedClient
            .Setup(c => c.GetSpeedProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeedProfile
            {
                Code = "S123",
                DownloadSpeedMbps = 1,
                UploadSpeedMbps = 1
            });

        _activationClient
            .Setup(c => c.ActivateAsync(It.IsAny<ActivationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ActivationServiceException("upstream down"));

        var sut = BuildSut();

        var act = async () => await sut.ProvisionAsync(BuildValidOrder(), CancellationToken.None);

        await act.Should().ThrowAsync<ActivationServiceException>();
    }

    [Fact]
    public async Task ProvisionAsync_Throws_OnNullOrder()
    {
        var sut = BuildSut();

        var act = async () => await sut.ProvisionAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProvisionAsync_PassesCancellationToken_ToBothClients()
    {
        using var cts = new CancellationTokenSource();

        _speedClient
            .Setup(c => c.GetSpeedProfileAsync(It.IsAny<string>(), cts.Token))
            .ReturnsAsync(new SpeedProfile
            {
                Code = "S123",
                DownloadSpeedMbps = 1,
                UploadSpeedMbps = 1
            });
        _activationClient
            .Setup(c => c.ActivateAsync(It.IsAny<ActivationRequest>(), cts.Token))
            .ReturnsAsync(new ActivationResponse
            {
                ActivationId = "A",
                Status = "ACTIVE",
                ActivatedAt = DateTimeOffset.UtcNow
            });

        var sut = BuildSut();
        await sut.ProvisionAsync(BuildValidOrder(), cts.Token);

        _speedClient.Verify(c => c.GetSpeedProfileAsync(It.IsAny<string>(), cts.Token), Times.Once);
        _activationClient.Verify(c => c.ActivateAsync(It.IsAny<ActivationRequest>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task ProvisionAsync_SendsCorrectlyConstructedActivationRequest_ToNetworkController()
    {
        // Arrange: a valid TM Forum order containing customer + speed code
        var order = BuildValidOrder();

        // The Network Infrastructure API resolves "S123" to a concrete profile
        _speedClient
            .Setup(c => c.GetSpeedProfileAsync("S123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeedProfile
            {
                Code = "S123",
                DownloadSpeedMbps = 250,
                UploadSpeedMbps = 50
            });

        // Capture the request handed to the Network Controller for inspection
        ActivationRequest? capturedRequest = null;
        _activationClient
            .Setup(c => c.ActivateAsync(It.IsAny<ActivationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ActivationRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new ActivationResponse
            {
                ActivationId = "ACT-1",
                Status = "ACTIVE",
                ActivatedAt = DateTimeOffset.UtcNow
            });

        // Act
        await BuildSut().ProvisionAsync(order, CancellationToken.None);

        // Assert: the Network Controller received customer details extracted from the
        // TM Forum servChar AND the resolved profile data from the Network Infrastructure API.
        capturedRequest.Should().NotBeNull();
        capturedRequest!.CustomerId.Should().Be("C1233");
        capturedRequest.CustomerAddress.Should().Be("1333 Amsterdam");
        capturedRequest.DownstreamSpeed.Should().Be("250");
        capturedRequest.UpstreamSpeed.Should().Be("50");

        _activationClient.Verify(
            c => c.ActivateAsync(It.IsAny<ActivationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
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
                    BuildChar(CharacteristicNames.CustomerAddress, "1333 Amsterdam"),
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