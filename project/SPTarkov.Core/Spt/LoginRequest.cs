using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.Spt;

public record LoginRequest
{
    [Required]
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";
}
