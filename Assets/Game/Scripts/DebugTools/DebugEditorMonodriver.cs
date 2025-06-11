using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class DebugEditorMonoDriver : MonoBehaviour
{
    private IUpdatable _handler;

    private FocusRoutineController _focusRoutineCtrl = null;

    [Inject]
    public void Construct( FocusRoutineController focusRoutineCtrl )
    {
        _focusRoutineCtrl = focusRoutineCtrl;
    }

    public void Init(IUpdatable handler)
    {
        _handler = handler;
    }

    void Update()
    {
        _handler?.Update();
    }
}