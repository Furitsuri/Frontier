namespace Frontier.Entities
{
    /// <summary>
    /// 戦闘時におけるキャラクターのアニメーションタイプ
    /// </summary>
    public enum COMBAT_ANIMATION_TYPE
    {
        CLOSED = 0,
        RANGED,
        PARRY,
        CLOSED_IN_PLACE,    // 相手へ駆け寄らずにその場で行う近接攻撃(InPlaceAttackSequence で使用)

        NUM,
    }
}