using System;
using System.Collections.Generic;

public class LocalizationService : ILocalizationService
{
    private Language _currentLanguage = Language.English;
    private Dictionary<Language, Dictionary<string, string>> _table = new()
    {
        {
            Language.English, new Dictionary<string, string>
            {
                { "", "Hello" },
                { "_", "Goodbye" },
                { "UI_STATUS_LEVEL", "Level" },
                { "UI_STATUS_HP", "HP" },
                { "UI_STATUS_MOVE", "Move" },
                { "UI_STATUS_JUMP", "Jump" },
                { "UI_STATUS_ACTION", "Action" },
                { "UI_STATUS_ATTACK", "Attack" },
                { "UI_STATUS_DEFFENCE", "Defence" },
                { "UI_CMD_MOVE", "Move" },
                { "UI_CMD_ATTACK", "Attack" },
                { "UI_CMD_SKILL", "Skill" },
                { "UI_CMD_WAIT", "Wait" },
                { "UI_CMD_USE_SKILL_OPTION_EXECUTION", "Execution" },
                { "UI_CMD_USE_SKILL_OPTION_QUEUE", "Queue" },
                { "UI_CMD_USE_SKILL_OPTION_COOPERATIVE", "Cooperative" },
                { "UI_CMD_RESERVED_ACTION_EXECUTE", "Execute" },
                { "UI_CMD_OPTION", "Option" },
                { "UI_CMD_TURN_END", "Turn End" },
            }
        },
        {
            Language.Japanese, new Dictionary<string, string>
            {
                { "", "Hola" },
                { "_", "Adiós" },
                { "UI_STATUS_LEVEL", "レベル" },
                { "UI_STATUS_HP", "体力" },
                { "UI_STATUS_MOVE", "移動力" },
                { "UI_STATUS_JUMP", "ジャンプ力" },
                { "UI_STATUS_ACTION", "アクション" },
                { "UI_STATUS_ATTACK", "攻撃力" },
                { "UI_STATUS_DEFFENCE", "防御力" },
                { "UI_CMD_MOVE", "移動" },
                { "UI_CMD_ATTACK", "攻撃" },
                { "UI_CMD_SKILL", "スキル" },
                { "UI_CMD_WAIT", "待機" },
                { "UI_CMD_USE_SKILL_OPTION_EXECUTION", "実行" },
                { "UI_CMD_USE_SKILL_OPTION_QUEUE", "連携予約" },
                { "UI_CMD_USE_SKILL_OPTION_COOPERATIVE", "連携" },
                { "UI_CMD_RESERVED_ACTION_EXECUTE", "実行" },
                { "UI_CMD_OPTION", "オプション" },
                { "UI_CMD_TURN_END", "ターン終了" },
            }
        }
    };
    public event Action OnLanguageChanged;

    public void ChangeLanguage( Language lang )
    {
        _currentLanguage = lang;
        OnLanguageChanged?.Invoke();
    }

    public string Get( string key )
    {
        return _table[_currentLanguage][key];
    }
}