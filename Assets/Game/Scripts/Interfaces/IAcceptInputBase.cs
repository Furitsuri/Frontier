/// <summary>
///  非ジェネリックの基底インターフェースです
///  ジェネリック型のIGetInputはそのまま配列として変数化出来ないため、
///  こちらを配列として扱うことで代用します
/// </summary>
public interface IAcceptInputBase
{
    // 引数をobject型にすることで汎用的に扱う
    bool AcceptInput( object obj );
}