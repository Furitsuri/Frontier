static public class Constants
{
    /// <summary>
    /// ステージ上の進行方向
    /// </summary>
    public enum Direction
    {
        NONE = -1,

        FORWARD,    // Z軸正方向
        RIGHT,      // X軸正方向
        BACK,       // Z軸負方向
        LEFT,       // X軸負方向

        NUM_MAX
    }

    /// <summary>
    /// 各入力キーの定義
    /// ゲーム中で使用する全てのキーをここで定義します
    /// </summary>
    public enum GuideIcon : int
    {
        ALL_CURSOR = 0,     // 全方向
        VERTICAL_CURSOR,    // 縦方向
        HORIZONTAL_CURSOR,  // 横方向
        CONFIRM,            // 決定
        CANCEL,             // 戻る
        ESCAPE,             // オプション
        SUB1,               // 入力サブ1
        SUB2,               // 入力サブ2
        SUB3,               // 入力サブ3
        SUB4,               // 入力サブ4

#if UNITY_EDITOR
        DEBUG_MENU,         // デバッグメニュー
#endif

        NUM_MAX
    }

    // プレイヤー、敵それぞれのキャラクター最大数
    public const int CHARACTER_MAX_NUM = 16;
    // キャラクターが装備出来るスキルの最大数
    public const int EQUIPABLE_SKILL_MAX_NUM = 4;
    // キャラクターのアクションゲージの最大数
    public const int ACTION_GAUGE_MAX = 10;
    // 1グリッドに隣接するグリッドの最大数
    public const int NEIGHBORING_GRID_MAX_NUM = 4;
    // 経路探索におけるルートインデックス最大保持数
    public const int DIJKSTRA_ROUTE_INDEXS_MAX_NUM = 256;
    // グリッドのY座標に加算する補正値
    public const float ADD_GRID_POS_Y = 0.02f;
    // キャラクターの移動速度
    public const float CHARACTER_MOVE_SPEED = 7.5f;
    // キャラクターの回転速度
    public const float CHARACTER_ROT_SPEED = 10f;
    // キャラクターの回転終了閾値
    public const float CHARACTER_ROT_THRESHOLD = 3f;
    // プレイヤーの移動操作時、目標座標に対し入力を受け付けられるグリッドサイズの割合
    public const float ACCEPTABLE_INPUT_GRID_SIZE_RATIO = 0.33f;
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
    // 方向に対する入力について、最後に入力操作を行ってから、次のキー操作が有効になるまでのインターバル時間
    public const float DIRECTION_INPUT_INTERVAL = 0.13f;
    public const float CONFIRM_INPUT_INTERVAL   = 0.0f;
    public const float CANCEL_INPUT_INTERVAL    = 0.0f;
    public const float OPTIONAL_INPUT_INTERVAL  = 0.0f;
    // 入力ガイドにおけるスプライトテキスト間の幅
    public const float SPRITE_TEXT_SPACING_ON_KEY_GUIDE = 10f;

    public const string LAYER_NAME_CHARACTER            = "Character";
    public const string LAYER_NAME_LEFT_PARAM_WINDOW    = "ParamRenderLeft";
    public const string LAYER_NAME_RIGHT_PARAM_WINDOW   = "ParamRenderRight";
    public const string OBJECT_TAG_NAME_CHARA_SKIN_MESH = "CharacterSkinMesh";
    public const string GUIDE_SPRITE_FOLDER_PASS        = "Sprites/Originals/UI/InputGuide/";
#if UNITY_EDITOR
    public const string GUIDE_SPRITE_FILE_NAME          = "Preview Keyboard & Mouse";
#elif UNITY_STANDALONE_WIN
    public const string GUIDE_SPRITE_FILE_NAME          = "Preview Steam Deck";
#else
    public const string GUIDE_SPRITE_FILE_NAME          = "Preview Steam Deck";
#endif
}