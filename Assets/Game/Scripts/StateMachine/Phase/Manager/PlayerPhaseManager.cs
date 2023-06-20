using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPhaseManager : PhaseManagerBase
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
        if( _isFirstUpdate )
        {
            // �t�F�[�Y�A�j���[�V�����̊J�n
            StartPhaseAnim();

            _isFirstUpdate = false;

            return false;
        }
        // �t�F�[�Y�A�j���[�V�������͑��얳��
        if( BattleUISystem.Instance.IsPlayingPhaseUI() )
        {
            return false;
        }

        return base.Update();
    }

    override protected void CreateTree()
    {
        // �J�ږ؂̍쐬
        // TODO : �ʂ̃t�@�C��(XML�Ȃ�)����ǂݍ���ō쐬�o����ƕ֗�

        RootNode = new PLSelectGrid();
        RootNode.AddChild(new PLSelectCommandState());
        RootNode.AddChild(new PLConfirmTurnEnd());
        RootNode.Children[0].AddChild(new PLMoveState());
        RootNode.Children[0].AddChild(new PLAttackState());
        RootNode.Children[0].AddChild(new PLWaitState());

        CurrentNode = RootNode;
    }

    /// <summary>
    /// �t�F�[�Y�A�j���[�V�������Đ����܂�
    /// </summary>
    override protected void StartPhaseAnim()
    {
        BattleUISystem.Instance.TogglePhaseUI(true, BattleManager.TurnType.PLAYER_TURN);
        BattleUISystem.Instance.StartAnimPhaseUI();
    }
}
