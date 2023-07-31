using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyPhaseManager : PhaseManagerBase
{
    /// <summary>
    /// ���������s���܂�
    /// </summary>
    override public void Init()
    {
        // �ڕW���W��U���Ώۂ����Z�b�g
        foreach ( Enemy enemy in BattleManager.Instance.GetEnemyEnumerable() )
        {
            enemy.EmAI.ResetDestinationAndTarget();
        }
        // MEMO : ��L���Z�b�g��ɏ���������K�v�����邽�߂ɂ��̈ʒu�ł��邱�Ƃɒ���
        base.Init();
        // �I���O���b�h��(1�Ԗڂ�)�G�̃O���b�h�ʒu�ɍ��킹��
        if (BattleManager.Instance.GetEnemyEnumerable() != null)
        {
            Enemy enemy = BattleManager.Instance.GetEnemyEnumerable().First();
            StageGrid.Instance.ApplyCurrentGrid2CharacterGrid(enemy);
        }
        // �A�N�V�����Q�[�W�̉�
        BattleManager.Instance.RecoveryActionGaugeForGroup(Character.CHARACTER_TAG.CHARACTER_ENEMY);
    }

    /// <summary>
    /// �X�V���s���܂�
    /// </summary>
    override public bool Update()
    {
        if (_isFirstUpdate)
        {
            // �t�F�[�Y�A�j���[�V�����̊J�n
            StartPhaseAnim();

            _isFirstUpdate = false;

            return false;
        }

        // �t�F�[�Y�A�j���[�V�������͑��얳��
        if (BattleUISystem.Instance.IsPlayingPhaseUI())
        {
            return false;
        }

        return base.Update();
    }

    override protected void CreateTree()
    {
        // �J�ږ؂̍쐬
        RootNode = new EMSelectState();
        RootNode.AddChild(new EMMoveState());
        RootNode.AddChild(new EMAttackState());
        RootNode.AddChild(new EMWaitState());

        CurrentNode = RootNode;
    }

    /// <summary>
    /// �t�F�[�Y�A�j���[�V�������Đ����܂�
    /// </summary>
    override protected void StartPhaseAnim()
    {
        BattleUISystem.Instance.TogglePhaseUI(true, BattleManager.TurnType.ENEMY_TURN);
        BattleUISystem.Instance.StartAnimPhaseUI();
    }
}
