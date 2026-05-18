using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WifiProvisioning.Core.Exceptions;
using WifiProvisioning.Core.Models.Domain;
using static WifiProvisioning.Core.Exceptions.ProvisioningException;

namespace WifiProvisioning.Core.Services;
/// <summary>
/// HTTP client for the Network Controller API.
/// Sends a synchronous activation request and surfaces failures as
/// <see cref="ActivationServiceException"/>.
/// </summary>
public sealed class ActivationClient : IActivationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ActivationClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ActivationClient(HttpClient httpClient, ILogger<ActivationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ActivationResponse> ActivateAsync(
        ActivationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Calling Activation API for customer {CustomerId}",
            request.CustomerId);

        var payload = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient
                .PostAsync("/activation", content, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Activation API call timed out");
            throw new ActivationServiceException("Activation API call timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling Activation API");
            throw new ActivationServiceException("Network error calling Activation API.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadBodyAsync(response).ConfigureAwait(false);
            _logger.LogError(
                "Activation API returned non-success status {StatusCode}. Body: {Body}",
                response.StatusCode, body);
            throw new ActivationServiceException(
                $"Activation API returned status {(int)response.StatusCode}.");
        }

        ActivationResponse? result;
        try
        {
            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            result = JsonSerializer.Deserialize<ActivationResponse>(responseBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Activation API response");
            throw new ActivationServiceException("Invalid JSON in Activation API response.", ex);
        }

        if (result is null)
        {
            _logger.LogError("Activation API returned a JSON null response");
            throw new ActivationServiceException("Activation API returned a null response body.");
        }

        if (string.IsNullOrWhiteSpace(result.ActivationId))
        {
            _logger.LogError("Activation API returned a response with no ActivationId");
            throw new ActivationServiceException("Activation API response missing ActivationId.");
        }

        _logger.LogInformation(
            "Activation succeeded: ActivationId={ActivationId}, Status={Status}",
            result.ActivationId, result.Status);

        return result;
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch
        {
            return "<unavailable>";
        }
    }
}