using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PhaseManagerBase
{
    protected PhaseStateBase m_RootState;
    protected PhaseStateBase m_CurrentState;

    virtual public void Init()
    {
        // 遷移木の作成
        CreateStateTree();

        m_CurrentState.Init();
    }

    virtual public void Update()
    {
        // 現在実行中のステートを更新
        m_CurrentState.Update();
    }

    virtual public void LateUpdate()
    {
        // ステートの遷移を監視
        int transitIndex = m_CurrentState.TransitIndex;
        if ( 0 <= transitIndex )
        {
            m_CurrentState.Exit();
            m_CurrentState = m_CurrentState.m_ChildStates[transitIndex];
            m_CurrentState.Init();
        }
        else if( m_CurrentState.IsBack() )
        {
            m_CurrentState.Exit();
            m_CurrentState = m_CurrentState.m_Parent;
            m_CurrentState.Init();
        }
    }

    // 遷移の木構造を作成
    virtual protected void CreateStateTree()
    {
    }
}
