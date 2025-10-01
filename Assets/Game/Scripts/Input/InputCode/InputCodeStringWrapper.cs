/// <summary>
/// InputCodeの説明文用のラッパークラスです
/// InputFacadeへInputCodeを渡したクラス側から文字列を変更できるようにするために使用します
/// </summary>
public class InputCodeStringWrapper
{
    public string Explanation { get; set; }

    public InputCodeStringWrapper(string explanation)
    {
        Explanation = explanation;
    }
}
