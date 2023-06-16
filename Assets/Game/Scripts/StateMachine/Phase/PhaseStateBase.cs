using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// MonoBehaviour‚ÍŒp³‚µ‚È‚¢
public class PhaseStateBase
{
    public PhaseStateBase m_Parent;
    public PhaseStateBase[] m_ChildStates;
    private bool m_isBack = false;
    public int TransitIndex { get; protected set; } = -1;

    // ‰Šú‰»
    virtual public void Init()
    {
        TransitIndex = -1;
        m_isBack = false;
    }

    // XV
    virtual public bool Update()
    {
        if( Input.GetKeyUp( KeyCode.Backspace ) )
        {
            Back();

            return true;
        }

        return false;
    }

    // ‘Ş”ğ
    virtual public void Exit()
    {
    }

    // –ß‚é
    virtual public bool IsBack()
    {
        return m_isBack;
    }

    /// <summary>
    /// e‚Ì‘JˆÚ‚É–ß‚è‚Ü‚·
    /// </summary>
    protected void Back()
    {
        m_isBack = true;
    }
}
