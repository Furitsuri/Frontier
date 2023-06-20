using UnityEngine;

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
        m_isBack = false;
    }

    // �X�V
    virtual public bool Update()
    {
        if( Input.GetKeyUp( KeyCode.Backspace ) )
        {
            Back();

            return true;
        }

        return false;
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

    /// <summary>
    /// �e�̑J�ڂɖ߂�܂�
    /// </summary>
    protected void Back()
    {
        m_isBack = true;
    }
}
