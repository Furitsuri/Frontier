/// <summary>
///  非ジェネリックの基底インターフェースです
///  ジェネリック型のIGetInputはそのまま配列として変数化出来ないため、
///  こちらを配列として扱うことで代用します
/// </summary>
public interface IGetInputBase
{
    // 返り値をobject型にすることで汎用的に扱う
    object GetInput();

    // modeはボタン系入力(GetBooleanInput)でのみ使用します。連続値系(方向・ベクトル)では無視されます
    void Apply( InputContext context, InputTriggerMode mode );
}