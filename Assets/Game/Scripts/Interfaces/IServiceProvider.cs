using System;

public interface IServiceProvider
{
    T GetService<T>();
}