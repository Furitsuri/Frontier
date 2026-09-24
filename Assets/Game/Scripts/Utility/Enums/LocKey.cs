/// <summary>
/// ローカライズ文字列を参照するためのキーです。
/// 実際の文言はResources/Localization/以下のJSONに定義されます。
/// </summary>
public enum LocKey
{
    None,

    // ステータス
    UI_STATUS_LEVEL,
    UI_STATUS_HP,
    UI_STATUS_MOVE,
    UI_STATUS_JUMP,
    UI_STATUS_ACTION,
    UI_STATUS_ATTACK,
    UI_STATUS_DEFFENCE,

    // コマンド
    UI_CMD_MOVE,
    UI_CMD_ATTACK,
    UI_CMD_SKILL,
    UI_CMD_WAIT,
    UI_CMD_USE_SKILL_OPTION_EXECUTION,
    UI_CMD_USE_SKILL_OPTION_QUEUE,
    UI_CMD_USE_SKILL_OPTION_COOPERATIVE,
    UI_CMD_RESERVED_ACTION_EXECUTE,
    UI_CMD_OPTION,
    UI_CMD_TURN_END,
    UI_CMD_TROOPS,
    UI_CMD_LEVEL_UP,
    UI_CMD_STATUS_UP,
    UI_CMD_SKILL_EQUIP,
    UI_CMD_SKILL_REMOVE,
    UI_CMD_SAVE,
    UI_CMD_LOAD,
    UI_CMD_DELETE,
    UI_CMD_EXIT_GAME,
    UI_CMD_NEW_GAME,
    UI_CMD_LOAD_GAME,

    // 確認ダイアログ
    UI_CONFIRM_YES,
    UI_CONFIRM_NO,
    UI_CONFIRM_EXIT_GAME_MESSAGE,
    UI_CONFIRM_DELETE_SAVE_MESSAGE,

    // スキル説明文
    EXPL_SKILL_DASH_SLASH,
    EXPL_SKILL_JUMP_SLASH,

    // 戦闘UI
    UI_BATTLE_ANIMA,

    // ステージリザルト画面
    UI_STAGE_RESULT_TITLE,
    UI_STAGE_RESULT_TURN,

    // 雇用フェーズ コマンド
    UI_CMD_EMPLOY,
    UI_CMD_DISMISS,

    // 解雇確認ダイアログ
    UI_CONFIRM_DISMISS_MEMBER_MESSAGE,

    // 雇用フェーズ 施設名
    UI_FACILITY_RECRUIT,

    // 雇用/解雇画面突入時の店主の会話
    UI_TALK_SHOPKEEPER_NAME,
    UI_TALK_EMPLOY_AVAILABLE,
    UI_TALK_EMPLOY_NONE_AVAILABLE,
    UI_TALK_DISMISS_AVAILABLE,
    UI_TALK_DISMISS_NONE_AVAILABLE,

    // 解雇完了確認ダイアログ(チェック済みメンバーをまとめて解雇)
    UI_CONFIRM_DISMISS_COMPLETED_MESSAGE,

    // 雇用/解雇完了確認(会話ウィンドウ形式、対象の単数/複数で文言が異なる)
    UI_TALK_EMPLOY_CONFIRM_SINGULAR,
    UI_TALK_EMPLOY_CONFIRM_PLURAL,
    UI_TALK_DISMISS_CONFIRM_SINGULAR,
    UI_TALK_DISMISS_CONFIRM_PLURAL,

    // フィールド画面
    // {0}にステージ番号(1オリジン)が入る書式文字列です
    UI_FIELD_STAGE_TITLE,

    // ショップ画面の退店時の店主の会話(退店確認/退店後の挨拶)
    UI_TALK_SHOP_LEAVE_CONFIRM,
    UI_TALK_SHOP_FAREWELL,

    // ショップ画面 施設名(ヘッダーの画面タイトル)/入店時の店主の挨拶
    UI_FACILITY_SHOP,
    UI_TALK_SHOP_GREETING,

    // ショップ画面の購入時の店主の会話(他の画面から戻った際の声かけ/購入個数の確認)
    UI_TALK_SHOP_ANYTHING_ELSE,
    UI_TALK_SHOP_ASK_QUANTITY,

    // ショップ画面の購入確認(会話ウィンドウ内のYes/No確認)
    UI_TALK_SHOP_PURCHASE_CONFIRM,
}
