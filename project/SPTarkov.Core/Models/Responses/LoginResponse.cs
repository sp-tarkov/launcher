namespace SPTarkov.Core.Models;

public record LoginResponse : ISptResponse<bool>
{
    public bool Response { get; set; }
}
