namespace Frontier.Stage
{
    [System.Flags]
    public enum TileBitFlag
    {
        NONE                    = 0,
        CANNOT_MOVE             = 1 << 0,   // 移動不可タイル
        REACHABLE_ATTACK        = 1 << 1,   // 攻撃が到達可能な立ち位置となるタイル
        ATTACKABLE              = 1 << 2,   // 攻撃可能なタイル
        ATTACKABLE_TARGET_EXIST = 1 << 3,   // 攻撃対象が存在しており、尚且つ攻撃可能なタイル
    }
}