namespace SPTarkov.Core.SPT.Responses;

public record SPTPingResponse : IResponse<string>
{
    public string? Response { get; set; } = "";
}
