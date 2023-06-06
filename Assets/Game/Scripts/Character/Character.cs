using System.Collections;
using System.Collections.Generic;
using System.Data;
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
    // キャラクターの持つパラメータ
    public struct Parameter
    {
        // キャラクター番号
        public int characterIndex;
        // ステージ開始時グリッド座標(インデックス)
        public int initGridIndex;
        // 移動レンジ
        public int moveRange;
        
        public Parameter( int charaIndex = -1, int gridIndex = -1, int range = 0 )
        {
            characterIndex  = charaIndex;
            initGridIndex   = gridIndex;
            moveRange       = range;
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
        ANIME_TAG_ATTACK,
        ANIME_TAG_NUM,
    }

    protected Animator animator;
    public Parameter param;
    public TmpParameter tmpParam;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        param = new Parameter();
        tmpParam = new TmpParameter();
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
