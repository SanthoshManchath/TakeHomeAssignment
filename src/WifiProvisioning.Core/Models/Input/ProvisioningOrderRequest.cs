using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WifiProvisioning.Core.Models.Input
{
    public class ProvisioningOrderRequest
    {
        [Required]
        [JsonPropertyName("externalId")]
        public string ExternalId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("orderItem")]
        public OrderItem OrderItem { get; set; } = new();
    }
}