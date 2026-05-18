namespace WifiProvisioning.Core.Models.Domain
{
    public class ProvisioningRequest
    {
        public string OrderId { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerAddress { get; set; } = string.Empty;

        public string SpeedProfile { get; set; } = string.Empty;

    }
}