using Microsoft.Extensions.Logging;
using WifiProvisioning.Core.Mapping;
using WifiProvisioning.Core.Models.Domain;
using WifiProvisioning.Core.Models.Input;

namespace WifiProvisioning.Core.Services;

/// <summary>
/// Orchestrates the WiFi provisioning workflow.
/// </summary>
/// <remarks>
/// <para>This orchestrator coordinates two downstream APIs:</para>
/// <list type="bullet">
///   <item>The <b>Network Infrastructure API</b> returns the catalog of
///         supported speed profiles. We resolve the customer's requested
///         speed code into a concrete profile.</item>
///   <item>The <b>Network Controller API</b> activates the service by
///         pushing configuration to network gear. We assume it responds
///         synchronously with the final activation status.</item>
/// </list>
/// </remarks>
public sealed class ProvisioningService : IProvisioningService
{
    private readonly ISpeedProfileClient _speedProfileClient;
    private readonly IActivationClient _activationClient;
    private readonly ILogger<ProvisioningService> _logger;

    public ProvisioningService(
        ISpeedProfileClient speedProfileClient,
        IActivationClient activationClient,
        ILogger<ProvisioningService> logger)
    {
        _speedProfileClient = speedProfileClient;
        _activationClient = activationClient;
        _logger = logger;
    }

    public async Task<ProvisioningResult> ProvisionAsync(
    ProvisioningOrderRequest provisioningOrderRequest,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provisioningOrderRequest);

        _logger.LogInformation(
            "Starting provisioning workflow for OrderId={OrderId}",
            provisioningOrderRequest.ExternalId);

        var provisioningRequest = provisioningOrderRequest.MapProvisioningRequest();

        _logger.LogInformation(
            "Order mapped: CustomerId={CustomerId}, SpeedCode={SpeedCode}",
            provisioningRequest.CustomerId, provisioningRequest.SpeedProfile);

        var profile = await _speedProfileClient
            .GetSpeedProfileAsync(provisioningRequest.SpeedProfile, cancellationToken)
            .ConfigureAwait(false);

        var activationRequest = new ActivationRequest
        {
            CustomerId = provisioningRequest.CustomerId,
            CustomerAddress = provisioningRequest.CustomerAddress,
            DownstreamSpeed = profile.DownloadSpeedMbps.ToString(),
            UpstreamSpeed = profile.UploadSpeedMbps.ToString()
        };

        var activation = await _activationClient
            .ActivateAsync(activationRequest, cancellationToken)
            .ConfigureAwait(false);

        var result = new ProvisioningResult
        {
            OrderId = provisioningRequest.OrderId,
            ActivationId = activation.ActivationId,
            Status = activation.Status,
            ProfileId = profile.Code,
            DownloadSpeedMbps = profile.DownloadSpeedMbps,
            UploadSpeedMbps = profile.UploadSpeedMbps,
            ActivatedAt = activation.ActivatedAt
        };

        _logger.LogInformation(
            "Provisioning complete: OrderId={OrderId}, ActivationId={ActivationId}, Status={Status}",
            result.OrderId, result.ActivationId, result.Status);

        return result;
    }
}