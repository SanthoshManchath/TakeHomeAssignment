namespace WifiProvisioning.Core.Models.Domain;

/// <summary>
/// Final outcome of a successful provisioning workflow, returned to the API caller.
/// </summary>
public sealed class ProvisioningResult
{
    public string OrderId { get; set; } = string.Empty;

    public string ActivationId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ProfileId { get; set; } = string.Empty;

    public int DownloadSpeedMbps { get; set; }

    public int UploadSpeedMbps { get; set; }

    public DateTimeOffset ActivatedAt { get; set; }
}