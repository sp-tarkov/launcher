namespace SPTarkov.Core.Models;

public class LoginToken
{
    public string Username { get; set; }

    public string Password { get; set; }

    public bool Toggle { get; set; }

    public long Timestamp { get; set; }
}
