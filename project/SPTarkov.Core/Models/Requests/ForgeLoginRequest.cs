using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public record ForgeLoginRequest
{
    [Required]
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [Required]
    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("token_name")]
    public string TokenName { get; set; } = "SPT Launcher Token";
}
