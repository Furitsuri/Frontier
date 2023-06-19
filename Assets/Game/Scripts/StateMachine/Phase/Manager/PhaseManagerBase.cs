using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PhaseManagerBase
{
    protected PhaseStateBase m_RootState;
    protected PhaseStateBase m_CurrentState;
    protected bool _isFirstUpdate = false;

    virtual public void Init()
    {
        // 遷移木の作成
        CreateStateTree();

        m_CurrentState.Init();

        _isFirstUpdate = true;
    }

    virtual public bool Update()
    {
        // 現在実行中のステートを更新
        if( m_CurrentState.Update() )
        {
            if( m_CurrentState.IsBack() && m_CurrentState.m_Parent == null )
            {
                return true;
            }
        }

        return false;
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

    /// <summary>
    /// フェーズアニメーションを再生します
    /// </summary>
    virtual protected void StartPhaseAnim()
    {
    }
}
