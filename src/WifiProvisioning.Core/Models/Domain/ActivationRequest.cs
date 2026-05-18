namespace WifiProvisioning.Core.Models.Domain;

public sealed class ActivationRequest
{
    public string CustomerId { get; set; } = string.Empty;

    public string CustomerAddress { get; set; } = string.Empty;

    public string UpstreamSpeed { get; set; } = string.Empty;

    public string DownstreamSpeed { get; set; }= string.Empty;
}