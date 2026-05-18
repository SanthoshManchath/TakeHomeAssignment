using AwesomeAssertions;
using WifiProvisioning.Core.Exceptions;
using WifiProvisioning.Core.Mapping;
using WifiProvisioning.Core.Models.Domain;
using WifiProvisioning.Core.Models.Input;
using Xunit;
using static WifiProvisioning.Core.Exceptions.ProvisioningException;

namespace WifiProvisioning.Tests.Mapping;

public sealed class ProvisioningRequestMapperTests
{
    [Fact]
    public void ToDomain_ReturnsExpectedDomainModel_ForValidOrder()
    {
        var order = BuildValidOrder();

        var result = order.MapProvisioningRequest();

        result.OrderId.Should().Be("123");
        result.CustomerId.Should().Be("C1233");
        result.CustomerName.Should().Be("John");
        result.CustomerAddress.Should().Be("1333 J Boston USA");
        result.SpeedProfile.Should().Be("S123");
    }

    [Fact]
    public void ToDomain_Throws_WhenOrderIsNull()
    {
        ProvisioningOrderRequest? order = null;

        var act = () => order!.MapProvisioningRequest();

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(CharacteristicNames.CustomerId)]
    [InlineData(CharacteristicNames.CustomerName)]
    [InlineData(CharacteristicNames.CustomerAddress)]
    [InlineData(CharacteristicNames.SpeedProfile)]
    public void ToDomain_Throws_WhenRequiredCharacteristicIsMissing(string missingName)
    {
        var order = BuildValidOrder();
        order.OrderItem.Service.ServiceCharacteristic = order.OrderItem.Service.ServiceCharacteristic
            .Where(c => c.Name != missingName)
            .ToList();

        var act = () => order.MapProvisioningRequest();

        act.Should().Throw<InvalidProvisioningOrderException>()
            .WithMessage($"*{missingName}*");
    }

    [Fact]
    public void ToDomain_Throws_WhenCharacteristicHasNoInnerValue()
    {
        var order = BuildValidOrder();
        var speed = order.OrderItem.Service.ServiceCharacteristic.First(c => c.Name == CharacteristicNames.SpeedProfile);
        speed.Value = new Dictionary<string, string> { ["@type"] = "string" };

        var act = () => order.MapProvisioningRequest();

        act.Should().Throw<InvalidProvisioningOrderException>()
            .WithMessage($"*{CharacteristicNames.SpeedProfile}*has no value*");
    }

    [Fact]
    public void ToDomain_Throws_WhenCharacteristicValueIsWhitespace()
    {
        var order = BuildValidOrder();
        var speed = order.OrderItem.Service.ServiceCharacteristic.First(c => c.Name == CharacteristicNames.SpeedProfile);
        speed.Value[CharacteristicNames.SpeedProfile] = "   ";

        var act = () => order.MapProvisioningRequest();

        act.Should().Throw<InvalidProvisioningOrderException>();
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