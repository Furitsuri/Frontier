namespace Frontier.Option
{
    /// <summary>
    /// オプション画面に表示される設定項目1件を表す共通インターフェース。
    /// スライダー・トグルなど、値の種類が異なる設定項目はこれを実装します。
    /// </summary>
    public interface IOptionItem
    {
        string Label { get; }
    }
}
