using UnityEngine;
using Frontier.DebugTools.DebugMenu;
using Frontier.DebugTools.StageEditor;

#if UNITY_EDITOR

public class DebugUISystem : MonoBehaviour
{
    [Header("DebugMenuUI")]
    public DebugMenuUI DebugMenuView;        // デバッグメニュー表示

    [Header("StageEditorUI")]
    public StageEditorUI StageEditorView;    // ステージエディット機能表示
}

#endif // UNITY_EDITOR