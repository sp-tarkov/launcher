namespace SPTarkov.Core.Models.Responses;

public record VersionResponse : ISptResponse<string>
{
    public required string? Response { get; set; }
}
