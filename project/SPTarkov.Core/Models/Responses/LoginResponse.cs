namespace SPTarkov.Core.Models.Responses;

public record LoginResponse : ISptResponse<bool>
{
    public bool Response { get; set; }
}
