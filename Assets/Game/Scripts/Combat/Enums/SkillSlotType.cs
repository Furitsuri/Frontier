namespace Frontier.Combat
{
    /// <summary>
    /// キャラクターの装備スキル枠の種類です。
    /// アクティブスキルはアクティブ枠(EQUIPABLE_ACTIVE_SKILL_MAX_NUM枠)にのみ、
    /// パッシブスキルはパッシブ枠(EQUIPABLE_PASSIVE_SKILL_MAX_NUM枠)にのみ装備出来ます。
    /// </summary>
    public enum SkillSlotType
    {
        ACTIVE = 0,     // 戦闘中に能動的に使用するスキルの枠
        PASSIVE,        // 装備しているだけで常時効果を持つスキルの枠
    }
}
