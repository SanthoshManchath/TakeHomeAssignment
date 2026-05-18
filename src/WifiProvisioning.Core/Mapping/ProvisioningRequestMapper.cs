using WifiProvisioning.Core.Models.Domain;
using WifiProvisioning.Core.Models.Input;
using static WifiProvisioning.Core.Exceptions.ProvisioningException;

namespace WifiProvisioning.Core.Mapping;

    public static class ProvisioningRequestMapper
    {
        public static ProvisioningRequest MapProvisioningRequest(this ProvisioningOrderRequest provisioningOrderRequest)
        {
            ArgumentNullException.ThrowIfNull(provisioningOrderRequest);

            var characteristics = provisioningOrderRequest.OrderItem.Service.ServiceCharacteristic;
            
            return new ProvisioningRequest
            {
                OrderId = provisioningOrderRequest.ExternalId,
                CustomerId = GetRequiredCharacteristicValue(characteristics, CharacteristicNames.CustomerId),
                CustomerName = GetRequiredCharacteristicValue(characteristics, CharacteristicNames.CustomerName),
                CustomerAddress = GetRequiredCharacteristicValue(characteristics, CharacteristicNames.CustomerAddress),
                SpeedProfile = GetRequiredCharacteristicValue(characteristics, CharacteristicNames.SpeedProfile)
            };
        }

private static string GetRequiredCharacteristicValue(
    List<ServiceCharacteristic> characteristics,
    string name)
{
    var match = characteristics.FirstOrDefault(c => c.Name == name);
    if (match is null)
    {
        throw new InvalidProvisioningOrderException(
            $"Required characteristic '{name}' is missing from servChar.");
    }

    if (!match.Value.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidProvisioningOrderException(
            $"Characteristic '{name}' has no value.");
    }

    return value;
}
    }
