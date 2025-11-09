namespace SPTarkov.Core.SPT;

public record SPTLoginResponse : IResponse<bool>
{
    public bool Response { get; set; }
}
