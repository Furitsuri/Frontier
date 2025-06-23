using Frontier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public interface IUiSystem
{
    public GeneralUISystem GeneralUi { get; }
    public BattleUISystem BattleUi { get; }
#if UNITY_EDITOR
    public DebugUISystem DebugUi { get; }
#endif // UNITY_EDITOR
    public void InitializeUiSystem();
}