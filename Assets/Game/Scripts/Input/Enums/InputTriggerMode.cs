/// <summary>
/// ボタン系入力(Is*Pressed)の判定タイミングを表します
/// </summary>
public enum InputTriggerMode
{
    Up,         // 離した瞬間に一度だけ受け付ける(既定・従来の挙動)
    Down,       // 押した瞬間に一度だけ受け付ける
    DownRepeat, // 押している間、InputCodeのインターバル毎に繰り返し受け付ける
}
