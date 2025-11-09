namespace SPTarkov.Core.SPT;

public record SPTPingResponse : IResponse<string>
{
    public string? Response { get; set; } = "";
}
