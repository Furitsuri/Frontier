public interface IServiceProvider
{
    T GetService<T>();
}