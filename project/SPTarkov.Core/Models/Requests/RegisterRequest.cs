using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class RegisterRequest : LoginRequest
{
    [Required]
    [JsonPropertyName("edition")]
    public string Edition { get; set; } = "";
}
