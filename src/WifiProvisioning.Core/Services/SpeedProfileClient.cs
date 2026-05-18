using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WifiProvisioning.Core.Exceptions;
using WifiProvisioning.Core.Models.Domain;
using static WifiProvisioning.Core.Exceptions.ProvisioningException;

namespace WifiProvisioning.Core.Services;
/// <summary>
/// HTTP client for the Network Infrastructure API.
/// Fetches the full speed-profile catalog, selects the one matching the
/// requested speed code, and surfaces failures as
/// <see cref="SpeedProfileServiceException"/> or
/// <see cref="SpeedNotAvailableException"/>.
/// </summary>
public class SpeedProfileClient : ISpeedProfileClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpeedProfileClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SpeedProfileClient(HttpClient httpClient, ILogger<SpeedProfileClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SpeedProfile> GetSpeedProfileAsync(
       string speedCode,
       CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching speed profile catalog to match speed code {SpeedCode}",
            speedCode);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient
                .GetAsync("/speed-profiles", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Speed Profile API call timed out");
            throw new SpeedProfileServiceException("Speed Profile API call timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling Speed Profile API");
            throw new SpeedProfileServiceException("Network error calling Speed Profile API.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadBodyAsync(response).ConfigureAwait(false);
            _logger.LogError(
                "Speed Profile API returned non-success status {StatusCode}. Body: {Body}",
                response.StatusCode, body);
            throw new SpeedProfileServiceException(
                $"Speed Profile API returned status {(int)response.StatusCode}.");
        }

         SpeedProfileResponse? speedProfileResponse;
    try
    {
        var content = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        speedProfileResponse = JsonSerializer.Deserialize<SpeedProfileResponse>(content, JsonOptions);
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Failed to deserialize Speed Profile API response");
        throw new SpeedProfileServiceException("Invalid JSON in Speed Profile API response.", ex);
    }

    if (speedProfileResponse is null)
    {
        _logger.LogError("Speed Profile API returned a JSON null response");
        throw new SpeedProfileServiceException("Speed Profile API returned a null response body.");
    }

    _logger.LogInformation(
        "Speed Profile API responded. CorrelationId={CorrelationId}, ProfileCount={Count}",
        speedProfileResponse.requestId, speedProfileResponse.SpeedProfiles.Count);

    var match = speedProfileResponse.SpeedProfiles
        .FirstOrDefault(p => string.Equals(p.Code, speedCode, StringComparison.Ordinal));

    if (match is null)
    {
        _logger.LogWarning(
            "No speed profile found matching speed code {SpeedCode}", speedCode);
        throw new SpeedNotAvailableException(
            $"No speed profile available for speed code '{speedCode}'.");
    }

    _logger.LogInformation(
        "Selected speed profile {Code} ({DownloadSpeed}/{UploadSpeed} Mbps)",
        match.Code, match.DownloadSpeedMbps, match.UploadSpeedMbps);

    return match;
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