/// <summary>
/// 入力インターフェ―スです
/// キーボードやゲームパッドなど、入力デバイスごとに切り替えます
/// </summary>
public interface IInput
{
    public Constants.Direction GetDirectionalPressed();

    public bool IsConfirmPressed();

    public bool IsCancelPressed();

    public bool IsToolPressed();

    public bool IsInfoPressed();

    public bool IsOptions1Pressed();

    public bool IsOptions2Pressed();

    public bool IsSub1Pressed();

    public bool IsSub2Pressed();

    public bool IsSub3Pressed();

    public bool IsSub4Pressed();

    public bool IsDebugMenuPressed();
}