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
        // �J�ږ؂̍쐬
        CreateStateTree();

        m_CurrentState.Init();

        _isFirstUpdate = true;
    }

    virtual public bool Update()
    {
        // ���ݎ��s���̃X�e�[�g���X�V
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
        // �X�e�[�g�̑J�ڂ��Ď�
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

    // �J�ڂ̖؍\�����쐬
    virtual protected void CreateStateTree()
    {
    }

    /// <summary>
    /// �t�F�[�Y�A�j���[�V�������Đ����܂�
    /// </summary>
    virtual protected void StartPhaseAnim()
    {
    }
}
