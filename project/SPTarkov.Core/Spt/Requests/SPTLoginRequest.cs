using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.SPT.Requests;

public record SPTLoginRequest
{
    [Required]
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";
}
