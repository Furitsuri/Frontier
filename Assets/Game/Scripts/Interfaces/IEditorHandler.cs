using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEditorHandler
{
    public void SetFocusRoutine( IFocusRoutine routine );
    public void Update();

    public void Run();

    public void Exit();
}
