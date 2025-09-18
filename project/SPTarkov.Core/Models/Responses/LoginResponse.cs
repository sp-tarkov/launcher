namespace SPTarkov.Core.Models;

public class LoginResponse : ISptResponse<bool>
{
    public bool Response { get; set; }
}
