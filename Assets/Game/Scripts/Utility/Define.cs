﻿using UnityEngine;

static public class Constants
{
    // プレイヤー、敵それぞれのキャラクター最大数
    public const int CHARACTER_MAX_NUM = 16;
    // キャラクターが装備出来るスキルの最大数
    public const int EQUIPABLE_SKILL_MAX_NUM = 4;
    // キャラクターのアクションゲージの最大数
    public const int ACTION_GAUGE_MAX = 10;
    // 1グリッドに隣接するグリッドの最大数
    public const int NEIGHBORING_GRID_MAX_NUM = 4;
    // 経路探索におけるルートインデックス最大保持数
    public const int DIJKSTRA_ROUTE_INDEXS_MAX_NUM = 255;
    // ステージに設定可能なタイルの最小数(横軸)
    public const int TILE_ROW_MIN_NUM       = 5;
    // ステージに設定可能なタイルの最小数(縦軸)
    public const int TILE_COLUMN_MIN_NUM    = 5;
    // ステージに設定可能なタイルの最大数(横軸)
    public const int TILE_ROW_MAX_NUM       = 25;
    // ステージに設定可能なタイルの最大数(縦軸)
    public const int TILE_COLUMN_MAX_NUM    = 25;
    // ジャンプ動作が必要と判定する高低差
    public const int NEED_JUMP_HEIGHT_DIFFERENCE = 1;
    // 降下する際に必要なジャンプ力に対して加算するマージン
    public const int DESCENT_MARGIN = 1;
    // ステータス異常のビットフラグにおいて、カテゴリーの区切りとなるビット数
    public const int STATUS_EFFECT_CATEGORY_BIT = 8;
    // タイルの一辺の長さ(タイルはすべて正方形)
    public const float TILE_SIZE = 1.0f;
    // タイルの厚みの最小値
    public const float TILE_MIN_THICKNESS   = 0.01f;
    // タイルの高さの最大値
    public const float TILE_MAX_HEIGHT      = 5.0f;
    // グリッドのY座標に加算する補正値
    public const float ADD_GRID_POS_Y       = 0.02f;
    // グリッドカーソルのY座標に加算する補正値
    public const float GRID_CURSOR_OFFSET_Y = 0.03f;
    // キャラクターの移動速度
    public const float CHARACTER_MOVE_SPEED = 6.5f;
    // キャラクターの高速移動時の移動速度レート
    public const float CHARACTER_MOVE_HIGH_SPEED_RATE = 3.0f;
    // キャラクターの回転速度
    public const float CHARACTER_ROT_SPEED = 10f;
    // キャラクターの回転終了閾値
    public const float CHARACTER_ROT_THRESHOLD = 3f;
    // キャラクターの向き判定を行う際に許容する角度誤差
    public const float CHARACTER_ANGLE_THRESHOLD_DEGREES = 5f;
    // プレイヤーの移動操作時、目標座標に対し入力を受け付けられるグリッドサイズの割合
    public const float ACCEPTABLE_INPUT_TILE_SIZE_RATIO = 0.33f;
    // ジャンプ動作時に対象のタイル位置が相対的に正のY座標に存在する場合に加速度に対して加算する値
    public const float JUMP_POSITIVE_Y_ACCELERATION = 10f;
    // ジャンプ動作時に対象のタイル位置が相対的に負のY座標に存在する場合の垂直方向の初速度
    public const float JUMP_NEGATIVE_Y_VELOCITY = 3f;
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
    // 毒ダメージの割合
    public const float POISON_DAMAGE_RATE = 0.1f;
    // 方向に対する入力について、最後に入力操作を行ってから、次のキー操作が有効になるまでのインターバル時間
    public const float GRID_DIRECTION_INPUT_INTERVAL    = 0.13f;
    public const float MENU_DIRECTION_INPUT_INTERVAL    = 0.23f;
    public const float CONFIRM_INPUT_INTERVAL           = 0.0f;
    public const float CANCEL_INPUT_INTERVAL            = 0.0f;
    public const float OPTIONAL_INPUT_INTERVAL          = 0.0f;
    public const float GUIDE_TEXT_MIN_SIZE              = 10f;
    public const float GUIDE_TEXT_MAX_SIZE              = 20f;
    // 入力ガイドにおけるスプライトテキスト間の幅
    public const float SPRITE_TEXT_SPACING_ON_KEY_GUIDE = 10f;

    public const string LAYER_NAME_CHARACTER                = "Character";
    public const string LAYER_NAME_LEFT_PARAM_WINDOW        = "ParamRenderLeft";
    public const string LAYER_NAME_RIGHT_PARAM_WINDOW       = "ParamRenderRight";
    public const string OBJECT_TAG_NAME_CHARA_SKIN_MESH     = "CharacterSkinMesh";
    public const string GUIDE_SPRITE_FOLDER_PASS            = "Sprites/Originals/UI/InputGuide/";
    public const string TILE_MATERIALS_FOLDER_PASS          = "Materials/Tile/";
#if UNITY_EDITOR
    public const string DEBUG_TRANSION_INPUT_HASH_STRING    = "DEBUG";
    public const string GUIDE_SPRITE_FILE_NAME              = "Preview Keyboard & Mouse";
#elif UNITY_STANDALONE_WIN
    public const string GUIDE_SPRITE_FILE_NAME              = "Preview Steam Deck";
#else
    public const string GUIDE_SPRITE_FILE_NAME              = "Preview Steam Deck";
#endif

    static public readonly float DOT_THRESHOLD = Mathf.Cos(CHARACTER_ANGLE_THRESHOLD_DEGREES * Mathf.Deg2Rad);
}