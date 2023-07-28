static public class Constants
{
    public enum Direction
    {
        NONE = -1,

        FORWARD,    // Z軸正方向
        RIGHT,      // X軸正方向
        BACK,       // Z軸負方向
        LEFT,       // X軸負方向

        NUM_MAX
    }

    // プレイヤー、敵それぞれのキャラクター最大数
    public const int CHARACTER_MAX_NUM = 16;
    // キャラクターのアクションゲージの最大数
    public const int ACTION_GAUGE_MAX = 10;
    // 1グリッドに隣接するグリッドの最大数
    public const int NEIGHBORING_GRID_MAX_NUM = 4;
    // 経路探索におけるルートインデックス最大保持数
    public const int DIJKSTRA_ROUTE_INDEXS_MAX_NUM = 256;
    // グリッドのY座標に加算する補正値
    public const float ADD_GRID_POS_Y = 0.02f;
    // キャラクターの移動速度
    public const float CHARACTER_MOVE_SPEED = 5.0f;
    // 敵が移動範囲を表示した後、実際に移動するまでの待ち時間
    public const float ENEMY_SHOW_MOVE_RANGE_TIME = 0.35f;
    // 攻撃時に向きを定める際の待ち時間
    public const float ATTACK_ROTATIION_TIME = 0.2f;
    // 攻撃時に相手に近接するまでの時間
    public const float ATTACK_CLOSING_TIME = 0.55f;
    // 攻撃後に相手から距離を取るまでの時間
    public const float ATTACK_DISTANCING_TIME = 0.23f;
    // 攻撃シーケンスにおける待ち時間
    public const float ATTACK_SEQUENCE_WAIT_TIME = 0.75f;
    // 攻撃シーケンスにおける攻撃開始までの待ち時間
    public const float ATTACK_SEQUENCE_WAIT_ATTACK_TIME = 0.5f;
    // 攻撃シーケンスにおける終了待ち時間
    public const float ATTACK_SEQUENCE_WAIT_END_TIME = 0.95f;
}
