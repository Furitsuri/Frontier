using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;

public class Character : MonoBehaviour
{
    public enum BaseCommand
    {
        COMMAND_MOVE = 0,
        COMMAND_ATTACK,
        COMMAND_WAIT,

        COMMAND_MAX_NUM,
    }
    public enum CHARACTER_TAG
    {
        CHARACTER_NONE = -1,
        CHARACTER_PLAYER,
        CHARACTER_ENEMY,
        CHARACTER_OTHER,
        CHARACTER_NUM,
    }

    public enum MOVE_TYPE
    {
        MOVE_TYPE_NORMAL = 0,
        MOVE_TYPE_HORSE,
        MOVE_TYPE_FLY,
        MOVE_TYPE_HEAVY,
        MOVE_TYPE_NUM,
    }

    public enum ANIME_TAG
    {
        ANIME_TAG_WAIT = 0,
        ANIME_TAG_MOVE,
        ANIME_TAG_ATTACK_01,
        ANIME_TAG_NUM,
    }

    // キャラクターの持つパラメータ
    public struct Parameter
    {
        // キャラクター番号
        public int characterIndex;
        // ステージ開始時グリッド座標(インデックス)
        public int initGridIndex;
        // ステージ開始時向き
        public Constants.Direction initDir;
        // 最大HP
        public int MaxHP;
        // 現在HP
        public int CurHP;
        // 攻撃力
        public int Atk;
        // 防御力
        public int Def;
        // 移動レンジ
        public int moveRange;
        // 攻撃レンジ(最小)
        public int attackRangeMin;
        // 攻撃レンジ(最大)
        public int attackRangeMax;
        // キャラクタータイプ
        public CHARACTER_TAG charaTag;
        // UI表示用カメラ長さ(Y方向)
        public float UICameraLengthY;
        // UI表示用カメラ長さ(Z方向)
        public float UICameraLengthZ;
        // UI表示用カメラターゲット(Y方向)
        public float UICameraLookAtCorrectY;

        public Parameter( int charaIndex = 0, int gridIndex = 0, int range = 0, Constants.Direction dir = Constants.Direction.FORWARD )
        {
            characterIndex  = charaIndex;
            initGridIndex   = gridIndex;
            moveRange       = range;
            attackRangeMin  = 1;
            attackRangeMax  = 1;
            MaxHP           = CurHP = 20;
            Atk             = 8;
            Def             = 5;
            initDir         = dir;
            charaTag        = CHARACTER_TAG.CHARACTER_NONE;
            UICameraLengthY = 1.2f;
            UICameraLengthZ = 1.5f;
            UICameraLookAtCorrectY = 1.0f;
        }
    }

    // 戦闘中のみ使用するパラメータ
    public struct TmpParameter
    {
        public bool[] isEndCommand;
        public int gridIndex;

        public TmpParameter(bool isEnd = false, int index = -1)
        {
            isEndCommand = new bool[(int)BaseCommand.COMMAND_MAX_NUM];
            for( int i = 0; i < (int)BaseCommand.COMMAND_MAX_NUM; ++i )
            {
                isEndCommand[i] = isEnd;
            }

            gridIndex = index;
        }
    }

    protected Animator animator;
    public Parameter param;
    public TmpParameter tmpParam;

    void Awake()
    {
        animator = GetComponent<Animator>();

        param = new Parameter(0, 0, 0, Constants.Direction.FORWARD);
        tmpParam = new TmpParameter(false, 0);
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector2[] moveableGrid()
    {

        return new Vector2[0];
    }

    virtual public void setAnimator( ANIME_TAG animTag, bool b)
    {

    }
}
