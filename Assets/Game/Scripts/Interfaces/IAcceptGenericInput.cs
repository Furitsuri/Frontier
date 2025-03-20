public interface IAcceptGenericInput<T>
{
    // 入力の対象に応じて戻り値を変更する
    public bool AcceptGenericInput( T arg );
}