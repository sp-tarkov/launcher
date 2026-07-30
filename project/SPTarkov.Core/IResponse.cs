namespace SPTarkov.Core;

public interface IResponse<T>
{
    public T? Response { get; set; }
}
