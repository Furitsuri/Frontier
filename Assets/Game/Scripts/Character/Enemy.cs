using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{
    /// <summary>
    /// �v�l�^�C�v
    /// </summary>
    public enum ThinkingType
    {
        NEAR = 0,   // �����̋����ɋ߂��G��D��

        NUM
    }

    public struct Plan
    {
        // �ړ��ڕW�O���b�h�C���f�b�N�X�l
        int destGridIndex;
        // �U���ڕW���j�b�g�C���f�b�N�X�l
        int targetCharaIndex;
    }

    public ThinkingType ThinkType { get; private set; }
    public EMAIBase EmAI { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        // TODO : ���^�]�p�Ƀp�����[�^���Z�b�g�B��قǍ폜
        this.param.characterTag = CHARACTER_TAG.CHARACTER_ENEMY;
        this.param.characterIndex = 5;
        this.param.moveRange = 2;
        this.param.initGridIndex = this.tmpParam.gridIndex = 13;
        this.param.MaxHP = this.param.CurHP = 8;
        this.param.Atk = 3;
        this.param.Def = 2;
        this.param.initDir = Constants.Direction.BACK;
        this.param.UICameraLengthY = 0.8f;
        this.param.UICameraLengthZ = 1.4f;
        this.param.UICameraLookAtCorrectY = 0.45f;
        BattleManager.Instance.AddEnemyToList(this);

        // �v�l�^�C�v�ɂ����emAI�ɑ������h���N���X��ύX����
        switch(ThinkType)
        {
            case ThinkingType.NEAR:
                EmAI = new EMAIAggressive();
                break;
            default:
                EmAI = new EMAIBase();
                break;
        }

        EmAI.Init(this);
    }

    override public void setAnimator(ANIME_TAG animTag)
    {
        _animator.SetTrigger(_animNames[(int)animTag]);
    }

    override public void setAnimator(ANIME_TAG animTag, bool b)
    {
        _animator.SetBool(_animNames[(int)animTag], b);
    }

    public override void Die()
    {
        base.Die();

        BattleManager.Instance.RemoveEnemyFromList(this);
        // Destroy(this);
    }

    /// <summary>
    /// �ړI���W�ƕW�I�L�����N�^�[�����肷��
    /// </summary>
    public (bool, bool) DetermineDestinationAndTargetWithAI()
    {
        return EmAI.DetermineDestinationAndTarget(param, tmpParam);
    }

    /// <summary>
    /// �ڕW���W�ƕW�I�L�����N�^�[���擾���܂�
    /// </summary>
    public void FetchDestinationAndTarget(out int destinationIndex, out Character targetCharacter)
    {
        destinationIndex    = EmAI.GetDestinationGridIndex();
        targetCharacter     = EmAI.GetTargetCharacter();
    }
}