using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WifiProvisioning.Core.Models.Input;

    public sealed class ServiceCharacteristic
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("valueType")]
        public string ValueType { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("value")]
        public Dictionary<string, string> Value { get; set; } = new();
    }