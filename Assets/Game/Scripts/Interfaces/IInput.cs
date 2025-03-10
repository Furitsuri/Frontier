/// <summary>
/// 入力インターフェ―スです
/// キーボードやゲームパッドなど、入力デバイスごとに切り替えます
/// </summary>
public interface IInput
{
    public Constants.Direction GetDirectionalPressed();

    public bool IsConfirmPressed();

    public bool IsCancelPressed();

    public bool IsPausePressed();

    public bool IsOptionsPressed();
}