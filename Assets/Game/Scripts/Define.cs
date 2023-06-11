static public class Constants
{
    public enum Direction
    {
        FORWARD,    // Z軸正方向
        RIGHT,      // X軸正方向
        BACK,       // Z軸負方向
        LEFT,       // X軸負方向

        NUM_MAX
    }

    // プレイヤー、敵それぞれのキャラクター最大数
    public const int CHARACTER_MAX_NUM = 16;
    // グリッドのY座標に加算する補正値
    public const float ADD_GRID_POS_Y = 0.02f;
    // キャラクターの移動速度
    public const float CHARACTER_MOVE_SPEED = 3.0f;
}
