using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models.Requests;

public record LoginRequest
{
    [Required]
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";
}
