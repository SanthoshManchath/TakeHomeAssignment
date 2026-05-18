using Microsoft.AspNetCore.Mvc;
using WifiProvisioning.Core.Models.Domain;
using WifiProvisioning.Core.Models.Input;
using WifiProvisioning.Core.Services;

namespace WifiProvisioning.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ProvisioningController : ControllerBase
{
    private readonly IProvisioningService _provisioningService;
    private readonly ILogger<ProvisioningController> _logger;

    public ProvisioningController(
        IProvisioningService provisioningService,
        ILogger<ProvisioningController> logger)
    {
        _provisioningService = provisioningService;
        _logger = logger;
    }

    /// <summary>
    /// Provisions a WiFi service for a customer.
    /// Receives a TM Forum-style order, orchestrates the workflow,
    /// and returns the final activation result.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProvisioningResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvisioningResult>> Provision(
        [FromBody] ProvisioningOrderRequest provisioningOrderRequest,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received provisioning request for OrderId={OrderId}",
            provisioningOrderRequest.ExternalId);

        var result = await _provisioningService
            .ProvisionAsync(provisioningOrderRequest, cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }
}