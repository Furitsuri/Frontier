using Frontier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontier.UI;

public class EditorUiSystem : MonoBehaviour, IUiSystem
{
    private GeneralUISystem _generarlUi = null;
#if UNITY_EDITOR
    private DebugUISystem _debugUi = null;
#endif // UNITY_EDITOR

    public GeneralUISystem GeneralUi => _generarlUi;
    public DeploymentUISystem DeployUi => null; // DeploymentUISystem is not defined in this context, returning null
    public BattleUISystem BattleUi => null; // BattleUISystem is not defined in this context, returning null
#if UNITY_EDITOR
    public DebugUISystem DebugUi => _debugUi;
#endif // UNITY_EDITOR

    /// <summary>
    /// UIのカテゴリを示すインデックス値です
    /// </summary>
    enum ChildIndex
    {
        General = 0,
#if UNITY_EDITOR
        Debug,
#endif // UNITY_EDITOR
    }

    // Start is called before the first frame update
    void Awake()
    {
        InitializeUiSystem();
    }

    public void InitializeUiSystem()
    {
        LazyInject.GetOrCreate( ref _generarlUi, () => transform.GetChild( ( int ) ChildIndex.General ).GetComponent<GeneralUISystem>() );
#if UNITY_EDITOR
        LazyInject.GetOrCreate( ref _debugUi, () => transform.GetChild( ( int ) ChildIndex.Debug ).GetComponent<DebugUISystem>() );

        _generarlUi.Setup();
#endif // UNITY_EDITOR
    }
}