namespace SPTarkov.Core.Models;

public record RemoveResponse : ISptResponse<bool>
{
    public List<MiniProfile> Profiles { get; set; } = [];

    public bool Response { get; set; }
}
