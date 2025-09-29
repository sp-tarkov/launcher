namespace SPTarkov.Core.Models;

public record PingResponse : ISptResponse<string>
{
    public string Response { get; set; } = "";
}
