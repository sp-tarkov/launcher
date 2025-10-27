namespace SPTarkov.Core.Spt;

public record LoginResponse : IResponse<bool>
{
    public bool Response { get; set; }
}
