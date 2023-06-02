using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPhaseManager : PhaseManagerBase
{
    override public void Update()
    {
        base.Update();
    }

    protected override void CreateStateTree()
    {
        // 遷移木の作成
        // TODO : 別のファイル(XMLなど)から読み込んで作成出来ると便利

        m_RootState = new PLSelectGrid();
        
        m_RootState.m_ChildStates = new PhaseStateBase[1];
        m_RootState.m_ChildStates[0] = new PLSelectCommandState();

        m_RootState.m_ChildStates[0].m_Parent           = m_RootState;
        m_RootState.m_ChildStates[0].m_ChildStates      = new PhaseStateBase[3];
        m_RootState.m_ChildStates[0].m_ChildStates[0]   = new PLMoveState();
        m_RootState.m_ChildStates[0].m_ChildStates[1]   = new PLAttackState();
        m_RootState.m_ChildStates[0].m_ChildStates[2]   = new PLWaitState();

        m_CurrentState = m_RootState;
    }
}
