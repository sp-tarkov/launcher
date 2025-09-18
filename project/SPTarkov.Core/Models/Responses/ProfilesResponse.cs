namespace SPTarkov.Core.Models;

public class ProfilesResponse : ISptResponse<List<MiniProfile>>
{
    public List<MiniProfile> Response { get; set; } = [];
}
