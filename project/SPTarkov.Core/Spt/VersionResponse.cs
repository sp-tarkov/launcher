namespace SPTarkov.Core.Spt;

public record VersionResponse : IResponse<string>
{
    public required string? Response { get; set; }
}
