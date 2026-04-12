namespace Frontier.Combat
{
    /// <summary>
    /// 攻撃可能な範囲に対し、どのタイルを攻撃対象とするかを区別する際に用います。
    /// </summary>
    public enum TargetingMode
    {
        NONE = -1,

        NORMAL_ATTACK,  // 通常攻撃
        CENTER,         // 特定のタイルを中心にTargetingValueの値分を展開した範囲
        DIRECTIONAL,    // キャラクターの向きに依存
        ALL,            // 範囲内すべて

        NUM,
    }
}