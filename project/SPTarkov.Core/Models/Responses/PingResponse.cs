namespace SPTarkov.Core.Models.Responses;

public record PingResponse : ISptResponse<string>
{
    public string? Response { get; set; } = "";
}
