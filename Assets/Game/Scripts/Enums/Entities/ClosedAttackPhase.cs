namespace Frontier.Entities
{
    /// <summary>
    /// 近接攻撃更新用フェイズ
    /// </summary>
    public enum CLOSED_ATTACK_PHASE
    {
        NONE = -1,

        CLOSINGE,
        ATTACK,
        DISTANCING,

        NUM,
    }
}