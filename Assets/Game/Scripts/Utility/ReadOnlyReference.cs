/// <summary>
/// 読取専用の参照ラッパーです。
/// readonlyフィールドを使用した場合、コンストラクタで初期化しなければならないため、DIコンテナ等での利用が困難になる場合があります。
/// 配列に対してはReadOnlyCollectionなどを使用することで対応できますが、単一のオブジェクトに対してはこのクラスを使用することで対応できます。
/// </summary>
/// <typeparam name="T"></typeparam>
public class ReadOnlyReference<T>
{
    private readonly T _value;
    public ReadOnlyReference( T value ) => _value = value;
    public T Value => _value;
}