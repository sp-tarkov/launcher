namespace SPTarkov.Core.Spt;

public record PingResponse : IResponse<string>
{
    public string? Response { get; set; } = "";
}
