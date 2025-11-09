using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.SPT;

public record SPTRegisterRequest : SPTLoginRequest
{
    [Required]
    [JsonPropertyName("edition")]
    public string Edition { get; set; } = "";
}
