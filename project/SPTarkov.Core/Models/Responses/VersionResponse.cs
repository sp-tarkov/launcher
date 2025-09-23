namespace SPTarkov.Core.Models;

public class VersionResponse : ISptResponse<string>
{
    public required string Response { get; set; }
}
