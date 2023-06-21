using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        WAIT = 0,
        MOVE,
        ATTACK_01,
        DAMAGED,

        ANIME_TAG_NUM,
    }

    // キャラクターの持つパラメータ
    public struct Parameter
    {
        // キャラクタータイプ
        public CHARACTER_TAG characterTag;
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
        // UI表示用カメラ長さ(Y方向)
        public float UICameraLengthY;
        // UI表示用カメラ長さ(Z方向)
        public float UICameraLengthZ;
        // UI表示用カメラターゲット(Y方向)
        public float UICameraLookAtCorrectY;

        public Parameter( int charaIndex = 0, int gridIndex = 0, int range = 0, Constants.Direction dir = Constants.Direction.FORWARD )
        {
            characterTag    = CHARACTER_TAG.CHARACTER_NONE;
            characterIndex  = charaIndex;
            initGridIndex   = gridIndex;
            moveRange       = range;
            attackRangeMin  = 1;
            attackRangeMax  = 1;
            MaxHP           = CurHP = 20;
            Atk             = 8;
            Def             = 5;
            initDir         = dir;
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
        public int expectedChangeHP;

        public TmpParameter(bool isEnd = false, int index = -1)
        {
            isEndCommand = new bool[(int)BaseCommand.COMMAND_MAX_NUM];
            for( int i = 0; i < (int)BaseCommand.COMMAND_MAX_NUM; ++i )
            {
                isEndCommand[i] = isEnd;
            }

            gridIndex = index;
            expectedChangeHP = 0;
        }

        public void Reset()
        {
            for( int i = 0; i < (int)BaseCommand.COMMAND_MAX_NUM; ++i )
            {
                isEndCommand[i] = false;
            }

            expectedChangeHP = 0;
        }
    }

    protected string[] _animNames =
    {
        "Wait",
        "Run",
        "Attack01",
        "GetHit"
    };

    protected Character _opponent;
    protected Animator _animator;
    protected Animation _animation;
    public Parameter param;
    public TmpParameter tmpParam;
    
    void Awake()
    {
        // タグとアニメーションの数は一致していること
        Debug.Assert( _animNames.Length == (int)ANIME_TAG.ANIME_TAG_NUM );

        _animator = GetComponent<Animator>();
        _animation = GetComponent<Animation>();

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

    virtual public void setAnimator(ANIME_TAG animTag)
    {

    }
    virtual public void setAnimator( ANIME_TAG animTag, bool b)
    {

    }

    /// <summary>
    /// 対戦相手を設定します
    /// </summary>
    /// <param name="opponent">対戦相手</param>
    public void SetOpponentCharacter( Character opponent )
    {
        _opponent = opponent;
    }

    /// <summary>
    /// 対戦相手の設定をリセットします
    /// </summary>
    public void ResetOpponentCharacter()
    {
        _opponent = null;
    }

    /// <summary>
    /// 指定のアニメーションが終了したかを判定します
    /// </summary>
    /// <param name="animTag">アニメーションタグ</param>
    /// <returns>true : 終了, false : 未終了</returns>
    public bool IsEndAnimation(ANIME_TAG animTag)
    {
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // animator側でHasExitTime(終了時間あり)をONにしている場合、終了時間を1.0に設定する必要があることに注意
        if (stateInfo.IsName(_animNames[(int)animTag]) && 1f <= stateInfo.normalizedTime)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 対戦相手にダメージを与えるイベントを発生させます
    /// ※攻撃アニメーションから呼び出し
    /// </summary>
    virtual public void AttackOpponentEvent()
    {
        if( _opponent == null )
        {
            Debug.Assert( false );
        }

        _opponent.setAnimator(ANIME_TAG.DAMAGED);
        // モーションと同時にHPを減らす
        _opponent.param.CurHP += _opponent.tmpParam.expectedChangeHP;
        // ダメージUIを表示
        BattleUISystem.Instance.ToggleDamageUI(true);
        BattleUISystem.Instance.SetDamageUIPosByCharaPos(_opponent, _opponent.tmpParam.expectedChangeHP);
    }
}
