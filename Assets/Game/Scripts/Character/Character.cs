using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[SerializeField]
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
        DIE,

        ANIME_TAG_NUM,
    }

    // キャラクターの持つパラメータ
    public struct Parameter
    {
        // キャラクタータイプ
        public CHARACTER_TAG characterTag;
        // キャラクター番号
        public int characterIndex;
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
        // 攻撃レンジ
        public int attackRange;
        // アクションゲージ最大値
        public int maxActionGauge;
        // アクションゲージ現在値
        public int curActionGauge;
        // アクションゲージ回復値
        public int recoveryActionGauge;
        // ステージ開始時グリッド座標(インデックス)
        public int initGridIndex;
        // ステージ開始時向き
        public Constants.Direction initDir;

        public Parameter( int charaIndex = 0, int gridIndex = 0, int range = 0, Constants.Direction dir = Constants.Direction.FORWARD )
        {
            characterTag                    = CHARACTER_TAG.CHARACTER_NONE;
            characterIndex                  = charaIndex;
            MaxHP = CurHP                   = 20;
            Atk                             = 8;
            Def                             = 5;
            moveRange                       = range;
            attackRange                     = 1;
            maxActionGauge = curActionGauge = 3;
            recoveryActionGauge             = 1;
            initGridIndex                   = gridIndex;
            initDir                         = dir;
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

    [System.Serializable]
    public struct CameraParameter
    {
        // UI表示用カメラ長さ(Y方向)
        public float UICameraLengthY;
        // UI表示用カメラ長さ(Z方向)
        public float UICameraLengthZ;
        // UI表示用カメラターゲット(Y方向)
        public float UICameraLookAtCorrectY;

        public CameraParameter( float lengthY, float lengthZ, float lookAtCorrectY )
        {
            UICameraLengthY         = lengthY;
            UICameraLengthZ         = lengthZ;
            UICameraLookAtCorrectY  = lookAtCorrectY;
        }
    }


    protected string[] _animNames =
    {
        "Wait",
        "Run",
        "Attack01",
        "GetHit",
        "Die"
    };

    protected Character _opponent;
    protected Animator _animator;
    protected Animation _animation;
    public Parameter param;
    public TmpParameter tmpParam;
    public CameraParameter camParam;
    
    void Awake()
    {
        // タグとアニメーションの数は一致していること
        Debug.Assert( _animNames.Length == (int)ANIME_TAG.ANIME_TAG_NUM );

        _animator   = GetComponent<Animator>();
        _animation  = GetComponent<Animation>();

        param       = new Parameter(0, 0, 0, Constants.Direction.FORWARD);
        tmpParam    = new TmpParameter(false, 0);
    }

    virtual public void Init()
    {
        tmpParam.gridIndex = param.initGridIndex;
    }

    virtual public void setAnimator(ANIME_TAG animTag) { }

    virtual public void setAnimator( ANIME_TAG animTag, bool b) { }

    virtual public void Die() { }

    public Character.Parameter LoadCharacterParameter(string path)
    {
        string dataStr = "";
        StreamReader reader = new StreamReader(Application.dataPath + path);
        dataStr = reader.ReadToEnd();
        reader.Close();

        return JsonUtility.FromJson<Character.Parameter>(dataStr);
    }

    /// <summary>
    /// 対戦相手にダメージを与えるイベントを発生させます
    /// ※攻撃アニメーションから呼び出し
    /// </summary>
    virtual public void AttackOpponentEvent()
    {
        if (_opponent == null)
        {
            Debug.Assert(false);
        }

        _opponent.param.CurHP += _opponent.tmpParam.expectedChangeHP;

        //　ダメージが0の場合はモーションを取らない
        if (_opponent.tmpParam.expectedChangeHP != 0)
        {
            if (_opponent.param.CurHP <= 0)
            {
                _opponent.param.CurHP = 0;
                _opponent.setAnimator(ANIME_TAG.DIE);
            }
            else
            {
                _opponent.setAnimator(ANIME_TAG.DAMAGED);
            }
        }

        // ダメージUIを表示
        BattleUISystem.Instance.SetDamageUIPosByCharaPos(_opponent, _opponent.tmpParam.expectedChangeHP);
        BattleUISystem.Instance.ToggleDamageUI(true);
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
    /// 死亡判定を返します
    /// </summary>
    /// <returns>死亡しているか否か</returns>
    public bool IsDead()
    {
        return param.CurHP <= 0;
    }

    public void Remove()
    {
        Destroy(gameObject);
        Destroy(this);
    }
}
