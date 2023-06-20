using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPhaseManager : PhaseManagerBase
{
    /// <summary>
    /// ���������s���܂�
    /// </summary>
    override public void Init()
    {
        base.Init();
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
