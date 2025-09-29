namespace SPTarkov.Core.Models;

public record VersionResponse : ISptResponse<string>
{
    public required string Response { get; set; }
}
