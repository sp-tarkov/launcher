namespace SPTarkov.Core.SPT.Responses;

public record SPTModPagesResponse : IResponse<List<ModPage>>
{
    public List<ModPage>? Response { get; set; } = [];
}
