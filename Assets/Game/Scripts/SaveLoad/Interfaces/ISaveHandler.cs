public interface ISaveHandler<T>
{
    void Save(T data);
    T Load();
}