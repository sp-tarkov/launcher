namespace SPTarkov.Core.Models;

public class VersionResponse : ISptResponse<SPTVersion>
{
    public SPTVersion Response { get; set; } = new();
}
