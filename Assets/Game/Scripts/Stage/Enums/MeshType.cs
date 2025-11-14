namespace Frontier.Stage
{
    public enum MeshType
    {
        MOVE    = 0,
        REACHABLE_ATTACK,
        ATTACKABLE,
        ATTACKABLE_TARGET_EXIST,
        ENEMIES_ATTACKABLE,
        OTHERS_ATTACKABLE,

        DEPLOYABLE,     // 配置可能(ステージ開始前の配置フェーズで使用)

        NUM
    }
}