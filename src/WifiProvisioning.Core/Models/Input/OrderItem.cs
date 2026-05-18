using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WifiProvisioning.Core.Models.Input
{
    public class OrderItem
    {
        [Required]
        [JsonPropertyName("id")]
        public string Id { get; set; }  = string.Empty;

        [Required]
        [JsonPropertyName("service")]
        public Service Service { get; set; } = new();
    }
}