using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugUISystem : MonoBehaviour
{
    [Header("DebugMenuPresenter")]
    public DebugMenuPresenter DebugMenuView;        // デバッグメニュー表示

    [Header("StageEditorPresenter")]
    public StageEditorPresenter StageEditorView;    // ステージエディット機能表示
}