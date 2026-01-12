namespace Frontier.Combat
{
    [System.Flags]
    public enum SkillBitFlag
    {
        NONE = 0,
        ABLE_TO_COOPERATE   = 1 << 0,   // 連携可能
        ADJUSTABLE_COST     = 1 << 1,   // コスト調節可能
    }
}