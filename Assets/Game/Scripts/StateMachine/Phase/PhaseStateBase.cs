using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

    // XV
    virtual public void Update()
    {

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
}
