using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public record LoginRequest
{
    [Required]
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [Required]
    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}
