using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models.Requests;

public record PasswordChangeRequest : LoginRequest
{
    [Required]
    [JsonPropertyName("change")]
    public string Change { get; set; } = "";
}
