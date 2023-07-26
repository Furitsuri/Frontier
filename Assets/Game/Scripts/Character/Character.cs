using System;
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

    public enum CLOSED_ATTACK_PHASE
    {
        NONE = -1,

        CLOSINGE,
        ATTACK,
        DISTANCING,

        CLOSED_ATTACK_PHASE_NUM,
    }

    // �L�����N�^�[�̎��p�����[�^
    public struct Parameter
    {
        // �L�����N�^�[�^�C�v
        public CHARACTER_TAG characterTag;
        // �L�����N�^�[�ԍ�
        public int characterIndex;
        // �ő�HP
        public int MaxHP;
        // ����HP
        public int CurHP;
        // �U����
        public int Atk;
        // �h���
        public int Def;
        // �ړ������W
        public int moveRange;
        // �U�������W
        public int attackRange;
        // �A�N�V�����Q�[�W�ő�l
        public int maxActionGauge;
        // �A�N�V�����Q�[�W���ݒl
        public int curActionGauge;
        // �A�N�V�����Q�[�W�񕜒l
        public int recoveryActionGauge;
        // �X�e�[�W�J�n���O���b�h���W(�C���f�b�N�X)
        public int initGridIndex;
        // �X�e�[�W�J�n������
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

    // �퓬���̂ݎg�p����p�����[�^
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
        // UI�\���p�J��������(Y����)
        public float UICameraLengthY;
        // UI�\���p�J��������(Z����)
        public float UICameraLengthZ;
        // UI�\���p�J�����^�[�Q�b�g(Y����)
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

    [SerializeField]
    private GameObject _bulletObject;
    private float _elapsedTime = 0f;
    private bool _isTransitNextPhaseCamera = false;
    protected Character _opponent;
    protected Bullet _bullet;
    protected Animator _animator;
    protected Animation _animation;
    protected CLOSED_ATTACK_PHASE _closingAttackPhase;
    public Parameter param;
    public TmpParameter tmpParam;
    public CameraParameter camParam;
    
    void Awake()
    {
        // �^�O�ƃA�j���[�V�����̐��͈�v���Ă��邱��
        Debug.Assert( _animNames.Length == (int)ANIME_TAG.ANIME_TAG_NUM );

        _animator   = GetComponent<Animator>();
        _animation  = GetComponent<Animation>();

        param       = new Parameter(0, 0, 0, Constants.Direction.FORWARD);
        tmpParam    = new TmpParameter(false, 0);

        // �e�I�u�W�F�N�g���ݒ肳��Ă���ΐ���
        // �g�p���܂Ŕ�A�N�e�B�u�ɂ���
        if(_bulletObject != null )
        {
            GameObject bulletObject = Instantiate(_bulletObject);
            if( bulletObject != null )
            {
                _bullet = bulletObject.GetComponent<Bullet>();
                bulletObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// �������������s���܂�
    /// </summary>
    virtual public void Init()
    {
        tmpParam.gridIndex  = param.initGridIndex;
        _elapsedTime        = 0f;
    }

    /// <summary>
    /// �A�j���[�V�������Đ����܂�
    /// </summary>
    /// <param name="animTag">�A�j���[�V�����^�O</param>
    virtual public void setAnimator(ANIME_TAG animTag) { }

    /// <summary>
    /// �A�j���[�V�������Đ����܂�
    /// </summary>
    /// <param name="animTag">�A�j���[�V�����^�O</param>
    /// <param name="b">�g���K�[�A�j���[�V�����ɑ΂��Ďg�p</param>
    virtual public void setAnimator( ANIME_TAG animTag, bool b) { }

    /// <summary>
    /// ���S�������s���܂�
    /// </summary>
    virtual public void Die() { }

    /// <summary>
    /// �ΐ푊��Ƀ_���[�W��^����C�x���g�𔭐������܂�
    /// ���U���A�j���[�V��������Ăяo��
    /// </summary>
    virtual public void AttackOpponentEvent()
    {
        if (_opponent == null)
        {
            Debug.Assert(false);
        }

        _opponent.param.CurHP += _opponent.tmpParam.expectedChangeHP;

        //�@�_���[�W��0�̏ꍇ�̓��[�V���������Ȃ�
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

        // �_���[�WUI��\��
        BattleUISystem.Instance.SetDamageUIPosByCharaPos(_opponent, _opponent.tmpParam.expectedChangeHP);
        BattleUISystem.Instance.ToggleDamageUI(true);
    }

    /// <summary>
    /// �e�𔭎˂��܂�
    /// �C�x���g�Ƃ��ă��[�V��������Ă΂�܂�
    /// </summary>
    virtual public void FireBullet()
    {
        if (_bullet == null || _opponent == null) return;

        _bullet.gameObject.SetActive(true);

        // �ˏo�n�_�A�ڕW�n�_�Ȃǂ�ݒ肵�Ēe�𔭎�
        var firingPoint = transform.position;
        firingPoint.y += camParam.UICameraLookAtCorrectY;
        _bullet.SetFiringPoint(firingPoint);
        var targetCoordinate = _opponent.transform.position;
        targetCoordinate.y += _opponent.camParam.UICameraLookAtCorrectY;
        _bullet.SetTargetCoordinate(targetCoordinate);
        var gridLength = StageGrid.Instance.CalcurateGridLength(tmpParam.gridIndex, _opponent.tmpParam.gridIndex);
        _bullet.SetFlightTimeFromGridLength( gridLength );

        _bullet.StartUpdateCoroutine(AttackOpponentEvent);

        // ���˂Ɠ����Ɏ��̃J�����ɑJ�ڂ�����
        _isTransitNextPhaseCamera = true;
    }

    /// <summary>
    /// �ߐڍU�����J�n���܂�
    /// </summary>
    public void PlayClosedAttack()
    {
        _closingAttackPhase = CLOSED_ATTACK_PHASE.CLOSINGE;
        _elapsedTime = 0f;

        setAnimator(Character.ANIME_TAG.MOVE, true);
    }

    /// <summary>
    /// �ߐڍU�����̗�����X�V���܂�
    /// </summary>
    /// <param name="departure">�ߐڍU���̊J�n�n�_</param>
    /// <param name="destination">�ߐڍU���̏I���n�_</param>
    public void UpdateClosedAttack( in Vector3 departure, in Vector3 destination )
    {
        if (GetBullet() != null) return; 

        float t = 0f;

        switch( _closingAttackPhase )
        {
            case CLOSED_ATTACK_PHASE.CLOSINGE:
                _elapsedTime += Time.deltaTime;
                t = Mathf.Clamp01(_elapsedTime / Constants.ATTACK_CLOSING_TIME);
                t = Mathf.SmoothStep(0f, 1f, t);
                gameObject.transform.position = Vector3.Lerp(departure, destination, t);
                if (1.0f <= t)
                {
                    _elapsedTime = 0f;
                    setAnimator(Character.ANIME_TAG.ATTACK_01);

                    _closingAttackPhase = CLOSED_ATTACK_PHASE.ATTACK;
                }
                break;
            case CLOSED_ATTACK_PHASE.ATTACK:
                if( IsPlayinghAnimation(Character.ANIME_TAG.WAIT) )
                {
                    setAnimator(Character.ANIME_TAG.MOVE, false);

                    _closingAttackPhase = CLOSED_ATTACK_PHASE.DISTANCING;
                }
                break;
            case CLOSED_ATTACK_PHASE.DISTANCING:
                // �U���O�̏ꏊ�ɖ߂�
                _elapsedTime += Time.deltaTime;
                t = Mathf.Clamp01(_elapsedTime / Constants.ATTACK_DISTANCING_TIME);
                t = Mathf.SmoothStep(0f, 1f, t);
                gameObject.transform.position = Vector3.Lerp(destination, departure, t);
                if (1.0f <= t)
                {
                    _elapsedTime = 0f;
                    _closingAttackPhase = CLOSED_ATTACK_PHASE.NONE;
                }
                break;
            default: break;
        }
    }

    /// <summary>
    /// �ΐ푊���ݒ肵�܂�
    /// </summary>
    /// <param name="opponent">�ΐ푊��</param>
    public void SetOpponentCharacter( Character opponent )
    {
        _opponent = opponent;
    }

    /// <summary>
    /// �ΐ푊��̐ݒ�����Z�b�g���܂�
    /// </summary>
    public void ResetOpponentCharacter()
    {
        _opponent = null;
    }

    public bool IsPlayer() { return param.characterTag == CHARACTER_TAG.CHARACTER_PLAYER; }

    public bool IsEnemy() { return param.characterTag == CHARACTER_TAG.CHARACTER_ENEMY; }

    public bool IsOther() { return param.characterTag == CHARACTER_TAG.CHARACTER_OTHER; }

    /// <summary>
    /// �w��̃A�j���[�V�������Đ������𔻒肵�܂�
    /// </summary>
    /// <param name="animTag">�A�j���[�V�����^�O</param>
    /// <returns>true : �Đ���, false : �Đ����Ă��Ȃ�</returns>
    public bool IsPlayinghAnimation(ANIME_TAG animTag)
    {
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // MEMO : animator����HasExitTime(�I�����Ԃ���)��ON�ɂ��Ă���ꍇ�A�I�����Ԃ�1.0�ɐݒ肷��K�v�����邱�Ƃɒ���
        if (stateInfo.IsName(_animNames[(int)animTag]) && stateInfo.normalizedTime < 1f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// �w��̃A�j���[�V�������I���������𔻒肵�܂�
    /// </summary>
    /// <param name="animTag">�A�j���[�V�����^�O</param>
    /// <returns>true : �I��, false : ���I��</returns>
    public bool IsEndAnimation(ANIME_TAG animTag)
    {
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // MEMO : animator����HasExitTime(�I�����Ԃ���)��ON�ɂ��Ă���ꍇ�A�I�����Ԃ�1.0�ɐݒ肷��K�v�����邱�Ƃɒ���
        if (stateInfo.IsName(_animNames[(int)animTag]) && 1f <= stateInfo.normalizedTime)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool IsTransitNextPhaseCamera()
    {
        return _isTransitNextPhaseCamera;
    }

    public void ResetTransitNextPhaseCamera()
    {
        _isTransitNextPhaseCamera = false;
    }

    /// <summary>
    /// ���S�����Ԃ��܂�
    /// </summary>
    /// <returns>���S���Ă��邩�ۂ�</returns>
    public bool IsDead()
    {
        return param.CurHP <= 0;
    }

    /// <summary>
    /// �Q�[���I�u�W�F�N�g���폜���܂�
    /// </summary>
    public void Remove()
    {
        Destroy(gameObject);
        Destroy(this);
    }

    /// <summary>
    /// �ݒ肳��Ă���e���擾���܂�
    /// </summary>
    /// <returns>Prefab�ɐݒ肳��Ă���e</returns>
    public Bullet GetBullet() { return _bullet; }
}