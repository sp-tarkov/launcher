namespace SPTarkov.Core.SPT.Responses;

public record SPTLoginResponse : IResponse<bool>
{
    public bool Response { get; set; }
}
