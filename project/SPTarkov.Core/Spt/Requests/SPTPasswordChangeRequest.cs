using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.SPT;

public record SPTPasswordChangeRequest : SPTLoginRequest
{
    [Required]
    [JsonPropertyName("change")]
    public string Change { get; set; } = "";
}
