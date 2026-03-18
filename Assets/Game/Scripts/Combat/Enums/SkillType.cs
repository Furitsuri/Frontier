namespace Frontier.Combat
{
    public enum ActionType : int
    {
        NONE = -1,

        BUFF,       // 自身へのバフ
        ATTACK,     // 対象への攻撃
        SUPPORT,    // 自軍へのサポート
        HEAL,       // 自軍への回復
        SPECIAL,    // 上記に該当しない特殊(固有)処理

        NUM,
    }
}