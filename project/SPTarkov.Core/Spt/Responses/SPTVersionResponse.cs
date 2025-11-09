namespace SPTarkov.Core.SPT;

public record SPTVersionResponse : IResponse<string>
{
    public required string? Response { get; set; }
}
