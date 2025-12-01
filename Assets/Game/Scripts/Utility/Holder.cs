/// <summary>
/// ラップ用のホルダークラスです
/// 参照型として値を保持したい場合などに用います
/// </summary>
/// <typeparam name="T"></typeparam>
public class Holder<T>
{
    public T Value;

    public Holder( T value  )
    {
        Value = value;
    }
}