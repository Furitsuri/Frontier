using static Constants;
/// <summary>
/// 各入力キーの定義
/// ゲーム中で使用する全てのキーをここで定義します
/// </summary>
public enum GuideIcon : int
{
    ALL_CURSOR          = 0,    // 全方向
    VERTICAL_CURSOR,            // 縦方向
    HORIZONTAL_CURSOR,          // 横方向
    CONFIRM,                    // 決定          PS : × / Xbox : A
    CANCEL,                     // 戻る          PS : ○ / Xbox : B
    TOOL,                       // ツール        PS : □ / Xbox : Y
    INFO,                       // 情報          PS : △ / Xbox : X
    OPT1,                       // オプション1   PS : START / Xbox : MENU
    OPT2,                       // オプション2   PS : SELECT / Xbox : VIEW
    SUB1,                       // 入力サブ1     PS : L1 / Xbox : LB   
    SUB2,                       // 入力サブ2     PS : R1 / Xbox : RB
    SUB3,                       // 入力サブ3     PS : L2 / Xbox : LT
    SUB4,                       // 入力サブ4     PS : R2 / Xbox : RT
    // SUB5,               // 入力サブ5     PS : L3 / Xbox : LS
    // SUB6,               // 入力サブ6     PS : R3 / Xbox : RS
    POINTER_MOVE,
    POINTER_LEFT,
    POINTER_RIGHT,
    POINTER_MIDDLE,

#if UNITY_EDITOR
    DEBUG_MENU,         // デバッグメニュー
#endif

    NUM_MAX
}