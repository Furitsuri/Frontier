public class ReadOnlyReference<T>
{
    private readonly T _value;
    public ReadOnlyReference( T value ) => _value = value;
    public T Value => _value;
}