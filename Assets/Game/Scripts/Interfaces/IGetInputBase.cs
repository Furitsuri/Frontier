/// <summary>
///  非ジェネリックの基底インターフェースです
///  ジェネリック型のIGetInputはそのまま配列として変数化出来ないため、
///  こちらを配列として扱うことで代用します
/// </summary>
public interface IGetInputBase
{
    object GetInput(); // 返り値をobject型にすることで汎用的に扱う
}