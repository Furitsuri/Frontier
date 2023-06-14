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

    // �L�����N�^�[�̎��p�����[�^
    public struct Parameter
    {
        // �L�����N�^�[�ԍ�
        public int characterIndex;
        // �X�e�[�W�J�n���O���b�h���W(�C���f�b�N�X)
        public int initGridIndex;
        // �X�e�[�W�J�n������
        public Constants.Direction initDir;
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
        // �U�������W(�ŏ�)
        public int attackRangeMin;
        // �U�������W(�ő�)
        public int attackRangeMax;
        // �L�����N�^�[�^�C�v
        public CHARACTER_TAG charaTag;
        // UI�\���p�J��������(Y����)
        public float UICameraLengthY;
        // UI�\���p�J��������(Z����)
        public float UICameraLengthZ;
        // UI�\���p�J�����^�[�Q�b�g(Y����)
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
    public Parameter param;
    public TmpParameter tmpParam;
    
    void Awake()
    {
        // �^�O�ƃA�j���[�V�����̐��͈�v���Ă��邱��
        Debug.Assert( _animNames.Length != (int)ANIME_TAG.ANIME_TAG_NUM );

        _animator = GetComponent<Animator>();

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

    virtual public void setAnimator(ANIME_TAG animTag)
    {

    }
    virtual public void setAnimator( ANIME_TAG animTag, bool b)
    {

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

    /// <summary>
    /// �ΐ푊��Ƀ_���[�W��^����C�x���g�𔭐������܂�
    /// ���U���A�j���[�V��������Ăяo��
    /// </summary>
    virtual public void AttackOpponentEvent()
    {
        if( _opponent == null )
        {
            Debug.Assert( false );
        }

        _opponent.setAnimator(ANIME_TAG.DAMAGED);
    }
}
