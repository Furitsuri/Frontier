namespace Frontier.Entities
{
    /// <summary>
    /// キャラクターの状態異常を表す列挙型
    /// </summary>
    public enum StatusEffect
    {
        NORMAL    = 0,      // 通常
        POISON,             // 毒
        SLEEP,              // 睡眠
        FLOATING,           // 浮遊
        INVISIBLE,          // 透明
        INVINCIBLE,         // 無敵

        NUM,
    }
}