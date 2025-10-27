using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.Spt;

public record RegisterRequest : LoginRequest
{
    [Required]
    [JsonPropertyName("edition")]
    public string Edition { get; set; } = "";
}
