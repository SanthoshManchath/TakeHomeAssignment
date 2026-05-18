using WifiProvisioning.Core.Models.Domain;

namespace WifiProvisioning.Core.Services;

/// <summary>
/// Client for the downstream Network Controller API.
/// Activates a customer's WiFi service by sending the resolved profile and
/// customer details so the controller can configure the underlying network gear.
/// </summary>
public interface IActivationClient
{
    Task<ActivationResponse> ActivateAsync(
        ActivationRequest request,
        CancellationToken cancellationToken);
}