using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class PasswordChangeRequest : LoginRequest
{
    [Required]
    [JsonPropertyName("change")]
    public string Change { get; set; } = "";
}
