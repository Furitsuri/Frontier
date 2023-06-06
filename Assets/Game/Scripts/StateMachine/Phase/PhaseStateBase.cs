using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// MonoBehaviour�͌p�����Ȃ�
public class PhaseStateBase
{
    public PhaseStateBase m_Parent;
    public PhaseStateBase[] m_ChildStates;
    private bool m_isBack = false;
    public int TransitIndex { get; protected set; } = -1;

    // ������
    virtual public void Init()
    {
        TransitIndex = -1;
    }

    // �X�V
    virtual public void Update()
    {

    }

    // �ޔ�
    virtual public void Exit()
    {
    }

    // �߂�
    virtual public bool IsBack()
    {
        return m_isBack;
    }
}
