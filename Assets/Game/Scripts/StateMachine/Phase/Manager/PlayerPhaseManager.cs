using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPhaseManager : PhaseManagerBase
{
    /// <summary>
    /// 初期化を行います
    /// </summary>
    override public void Init()
    {
        base.Init();
    }

    /// <summary>
    /// 更新を行います
    /// </summary>
    override public bool Update()
    {
        if( _isFirstUpdate )
        {
            // フェーズアニメーションの開始
            StartPhaseAnim();

            _isFirstUpdate = false;

            return false;
        }
        // フェーズアニメーション中は操作無効
        if( BattleUISystem.Instance.IsPlayingPhaseUI() )
        {
            return false;
        }

        return base.Update();
    }

    override protected void CreateStateTree()
    {
        // 遷移木の作成
        // TODO : 別のファイル(XMLなど)から読み込んで作成出来ると便利

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
    /// フェーズアニメーションを再生します
    /// </summary>
    override protected void StartPhaseAnim()
    {
        BattleUISystem.Instance.TogglePhaseUI(true, BattleManager.TurnType.PLAYER_TURN);
        BattleUISystem.Instance.StartAnimPhaseUI();
    }
}
