/// <summary>
/// 各入力キーの定義
/// ゲーム中で使用する全てのキーをここで定義します
/// </summary>
public enum GuideIcon : int
{
    ALL_CURSOR = 0,     // 全方向
    VERTICAL_CURSOR,    // 縦方向
    HORIZONTAL_CURSOR,  // 横方向
    CONFIRM,            // 決定
    CANCEL,             // 戻る
    TOOL,               // ツール
    INFO,               // 情報
    OPT1,               // オプション1
    OPT2,               // オプション2
    SUB1,               // 入力サブ1
    SUB2,               // 入力サブ2
    SUB3,               // 入力サブ3
    SUB4,               // 入力サブ4

#if UNITY_EDITOR
    DEBUG_MENU,         // デバッグメニュー
#endif

    NUM_MAX
}