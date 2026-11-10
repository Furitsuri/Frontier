namespace Frontier.Combat
{
    /// <summary>
    /// スキル等の効果の対象とする勢力です
    /// </summary>
    public enum TargetFaction
    {
        OPPONENT,   // 敵対勢力(攻撃系のスキル)
        ALLY,       // 自軍(使用者自身を含む。回復・サポート系のスキル)
    }
}
