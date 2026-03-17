/// <summary>
/// 各ルーチンの優先度です
/// 値の低いものから優先度が高くなります(ただしNONEは無効)
/// </summary>
public enum FocusRoutinePriority
{
    NONE = -1,

    DEBUG_EDITOR,       // デバッグエディター
    DEBUG_MENU,         // デバッグメニュー
    EVENT,              // イベント
    TUTORIAL,           // チュートリアル
    BATTLE_SKILL_EVENT, // 戦闘スキルイベント
    SEQUENCE,           // シーケンス
    MAIN_FLOW,          // メインフロー

    NUM,
}