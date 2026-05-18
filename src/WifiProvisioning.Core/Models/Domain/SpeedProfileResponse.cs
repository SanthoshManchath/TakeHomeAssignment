namespace WifiProvisioning.Core.Models.Domain;

/// <summary>
/// Envelope returned by the Speed Profile catalog API.
/// The <see cref="Id"/> is a correlation id we log but don't propagate.
/// </summary>
public sealed class SpeedProfileResponse
{
    public string requestId { get; set; } = string.Empty;

    public List<SpeedProfile> SpeedProfiles { get; set; } = new();
}