using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.SPT.Requests;

public record SPTPasswordChangeRequest : SPTLoginRequest
{
    [Required]
    [JsonPropertyName("change")]
    public string Change { get; set; } = "";
}
