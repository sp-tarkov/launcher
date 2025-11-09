using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.SPT;

public record SPTLoginRequest
{
    [Required]
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";
}
