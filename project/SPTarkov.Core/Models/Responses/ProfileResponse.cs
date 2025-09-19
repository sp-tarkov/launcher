namespace SPTarkov.Core.Models;

public class ProfileResponse : ISptResponse<MiniProfile>
{
    public MiniProfile? Response { get; set; }
}
