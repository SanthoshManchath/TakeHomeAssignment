using WifiProvisioning.Core.Models.Domain;
using WifiProvisioning.Core.Models.Input;

namespace WifiProvisioning.Core.Services;

/// <summary>
/// Orchestrates the WiFi provisioning workflow:
///   1. Resolve the requested speed code to an actual speed profile.
///   2. Activate using the profile + customer details.
///   3. Return a flat <see cref="ProvisioningResult"/> to the caller.
/// </summary>
public interface IProvisioningService
{
    Task<ProvisioningResult> ProvisionAsync(
        ProvisioningOrderRequest order,
        CancellationToken cancellationToken);
}