using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WifiProvisioning.Core.Models.Input;

    public sealed class ServiceSpecification
    {
        [Required]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
