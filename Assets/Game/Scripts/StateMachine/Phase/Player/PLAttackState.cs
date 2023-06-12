using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BattleManager;

public class PLAttackState : PhaseStateBase
{
    private enum PLAttackPhase
    {
        PL_ATTACK_SELECT_GRID = 0,
        PL_ATTACK_EXECUTE,
        PL_ATTACK_END,
    }

    private PLAttackPhase _phase = PLAttackPhase.PL_ATTACK_SELECT_GRID;
    private Character _attackCharacter = null;
    private Character _targetCharacter = null;
    private int _curentGridIndex = -1;
    private List<int> _attackableGrids = new List<int>(64);
    private CharacterAttackSequence _attackSequence = new CharacterAttackSequence();

    override public void Init()
    {
        var stgInstance = StageGrid.instance;
        var btlInstance = BattleManager.instance;

        base.Init();

        _phase         = PLAttackPhase.PL_ATTACK_SELECT_GRID;
        _curentGridIndex = stgInstance.currentGrid.GetIndex();

        // ���ݑI�𒆂̃L�����N�^�[�����擾���čU���͈͂�\��
        _attackCharacter = btlInstance.SearchPlayerFromCharaIndex(btlInstance.SelectCharacterIndex);
        if (_attackCharacter == null)
        {
            Debug.Assert(false, "SelectPlayer Irregular.");
            return;
        }
        var param = _attackCharacter.param;
        stgInstance.DrawAttackableGrids(_curentGridIndex, param.attackRangeMin, param.attackRangeMax);

        // �U���\�ȃO���b�h���ɓG�������ꍇ�ɕW�I�O���b�h�����킹��
        stgInstance.ApplyAttackTargetGridIndexs(0);
        // �A�^�b�J�[�L�����N�^�[�̐ݒ�
        btlInstance.SetAttackerCharacter( _attackCharacter );
    }

    public override void Update()
    {
        var btlInstance = BattleManager.instance;

        base.Update();

        switch(_phase)
        {
            case PLAttackPhase.PL_ATTACK_SELECT_GRID:
                // �O���b�h�̑���
                StageGrid.instance.OperateCurrentGrid();

                if (Input.GetKeyUp(KeyCode.Space))
                {
                    // �I�������L�����N�^�[���G�ł���ꍇ�͍U���J�n
                    var character = btlInstance.GetSelectCharacter();
                    if( character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_ENEMY )
                    {
                        // _targetCharacter�ɑ��
                        _targetCharacter = character;
                        _attackSequence.Init(_attackCharacter, _targetCharacter);

                        _phase = PLAttackPhase.PL_ATTACK_EXECUTE;
                    }
                }
                break;
            case PLAttackPhase.PL_ATTACK_EXECUTE:

                if ( _attackSequence.Update() )
                {
                    _phase = PLAttackPhase.PL_ATTACK_END;
                }
                
                break;
            case PLAttackPhase.PL_ATTACK_END:
                // �A�^�b�J�[�L�����N�^�[�̐ݒ������
                btlInstance.ResetAttackerCharacter();

                break;

        }
    }
}
