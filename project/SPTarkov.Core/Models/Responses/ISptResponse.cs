namespace SPTarkov.Core.Models;

public interface ISptResponse<T>
{
    public T Response { get; set; }
}
