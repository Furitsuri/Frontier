using UnityEngine;
using Frontier.DebugTools.Battle;
using Frontier.DebugTools.DebugMenu;
using Frontier.DebugTools.StageEditor;

#if UNITY_EDITOR

public class DebugUISystem : MonoBehaviour
{
    [Header("DebugMenuUI")]
    public DebugMenuUI DebugMenuView;        // デバッグメニュー表示

    [Header("StageEditorUI")]
    public StageEditorUI StageEditorView;    // ステージエディット機能表示

    [Header("DebugCharacterLabelUI")]
    [Tooltip("キャラクター頭上のデバッグ用ラベル表示。デバッグメニューを閉じている間も表示するため、DebugUIとは別のキャンバスに配置したものを参照する(戦闘を扱わないシーンでは未設定でよい)")]
    public DebugCharacterLabelUI CharacterLabelView;
}

#endif // UNITY_EDITOR