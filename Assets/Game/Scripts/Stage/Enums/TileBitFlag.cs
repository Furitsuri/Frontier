namespace Frontier.Stage
{
    [System.Flags]
    public enum TileBitFlag
    {
        NONE                    = 0,
        CANNOT_MOVE             = 1 << 0,   // 移動不可グリッド
        REACHABLE_ATTACK        = 1 << 1,   // 攻撃が到達可能なグリッド
        ATTACKABLE              = 1 << 2,   // 攻撃対象を攻撃可能なグリッド(ATTACKABLEの内容を実質含んでいる)
        ATTACKABLE_TARGET_EXIST = 1 << 3,   // 攻撃対象が存在しており、尚且つ攻撃が可能なグリッド
        ALLY_EXIST              = 1 << 4,   // 味方キャラクターが存在
        ENEMY_EXIST             = 1 << 5,   // 敵キャラクターが存在
        OTHER_EXIST             = 1 << 6,   // 第三勢力が存在
    }
}