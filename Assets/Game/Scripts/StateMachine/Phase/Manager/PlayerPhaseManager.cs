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

    override protected void CreateStateTree()
    {
        // �J�ږ؂̍쐬
        // TODO : �ʂ̃t�@�C��(XML�Ȃ�)����ǂݍ���ō쐬�o����ƕ֗�

        m_RootState = new PLSelectGrid();
        
        m_RootState.m_ChildStates = new PhaseStateBase[2];
        m_RootState.m_ChildStates[0] = new PLSelectCommandState();
        m_RootState.m_ChildStates[1] = new PLConfirmTurnEnd();

        m_RootState.m_ChildStates[0].m_Parent           = m_RootState;
        m_RootState.m_ChildStates[1].m_Parent           = m_RootState;
        m_RootState.m_ChildStates[0].m_ChildStates      = new PhaseStateBase[3];
        m_RootState.m_ChildStates[0].m_ChildStates[0]   = new PLMoveState();
        m_RootState.m_ChildStates[0].m_ChildStates[1]   = new PLAttackState();
        m_RootState.m_ChildStates[0].m_ChildStates[2]   = new PLWaitState();
        m_RootState.m_ChildStates[0].m_ChildStates[0].m_Parent = m_RootState.m_ChildStates[0];
        m_RootState.m_ChildStates[0].m_ChildStates[1].m_Parent = m_RootState.m_ChildStates[0];
        m_RootState.m_ChildStates[0].m_ChildStates[2].m_Parent = m_RootState.m_ChildStates[0];

        m_CurrentState = m_RootState;
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
