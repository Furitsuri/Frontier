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
        Transform childGeneralUI = transform.GetChild( ( int ) ChildIndex.General );
        if( childGeneralUI != null )
        {
            _generarlUi = childGeneralUI.GetComponent<GeneralUISystem>();
        }
        Debug.Assert( _generarlUi != null );

#if UNITY_EDITOR
        Transform childDebugUI = transform.GetChild( ( int ) ChildIndex.Debug );
        if( childDebugUI != null )
        {
            _debugUi = childDebugUI.GetComponent<DebugUISystem>();
        }
        Debug.Assert( _debugUi != null );
#endif // UNITY_EDITOR
    }
}