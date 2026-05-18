namespace WifiProvisioning.Core.Models.Domain;

public sealed class ActivationResponse
{
    public string ActivationId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset ActivatedAt { get; set; }
}