using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WifiProvisioning.Core.Models.Input;

public sealed class Service
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("serviceSpecification")]
    public ServiceSpecification ServiceSpecification { get; set; } = new();

    [Required]
    [JsonPropertyName("serviceCharacteristic")]
    public List<ServiceCharacteristic> ServiceCharacteristic { get; set; } = new();

}