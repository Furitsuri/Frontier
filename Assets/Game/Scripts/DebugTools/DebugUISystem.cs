using UnityEngine;
using Frontier.DebugTools.StageEditor;

#if UNITY_EDITOR

public class DebugUISystem : MonoBehaviour
{
    [Header("DebugMenuPresenter")]
    public DebugMenuPresenter DebugMenuView;        // デバッグメニュー表示

    [Header("StageEditorPresenter")]
    public StageEditorPresenter StageEditorView;    // ステージエディット機能表示
}

#endif // UNITY_EDITOR