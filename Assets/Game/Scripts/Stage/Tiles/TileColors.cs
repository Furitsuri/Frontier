using UnityEngine;
using Frontier.Stage;

public static class TileColors
{
    public static readonly Color[] Colors = new Color[( int ) MeshType.NUM]
    {
        new Color(0f,   0f,     1f, 0.65f), // 移動可能なタイル
        new Color(1f,   1f,     0f, 0.65f), // 攻撃が到達可能な立ち位置となるタイル( TileBitFlag.REACHABLE_ATTACK )
        new Color(1f,   0f,     0f, 0.65f), // 攻撃可能なタイル( TileBitFlag.ATTACKABLE )
        new Color(1f,   0f,     0f, 0.95f), // 攻撃可能なタイルで、尚且つ攻撃対象が存在している( ATTACKABLE_TARGET_EXIST )
        new Color(0.5f, 0f,     0f, 0.65f), // 敵が攻撃可能なタイル( ENEMIES_ATTACKABLE )
        new Color(0.5f, 0.5f, 0.5f, 0.65f), // 第三勢力が攻撃可能なタイル( OTHERS_ATTACKABLE )
    };
}