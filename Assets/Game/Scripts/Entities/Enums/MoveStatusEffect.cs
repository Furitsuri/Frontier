namespace Frontier.Entities
{
    /// <summary>
    /// キャラクターの移動系の状態異常を表す列挙型
    /// </summary>
    public enum MoveStatusEffect
    {
        FLOATING        = 0,    // 浮遊
        HYPER_GRAVITY,          // 過重力

        NUM,
    }
}