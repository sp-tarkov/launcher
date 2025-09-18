namespace SPTarkov.Core.Models;

public class PasswordChangeResponse : ISptResponse<bool>
{
    public List<MiniProfile> Profiles { get; set; } = [];

    public bool Response { get; set; }
}
