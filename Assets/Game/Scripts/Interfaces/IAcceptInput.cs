public interface IAcceptInput<T>
{
    // 入力の対象に応じて戻り値を変更する
    public void Accept( T t );
}