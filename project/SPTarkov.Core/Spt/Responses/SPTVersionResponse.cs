namespace SPTarkov.Core.SPT.Responses;

public record SPTVersionResponse : IResponse<string>
{
    public required string? Response { get; set; }
}
