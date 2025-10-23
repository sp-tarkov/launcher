namespace SPTarkov.Core.Models.Responses;

public interface ISptResponse<T>
{
    public T? Response { get; set; }
}
