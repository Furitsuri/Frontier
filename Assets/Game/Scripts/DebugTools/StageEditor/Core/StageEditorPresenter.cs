using Frontier.DebugTools.StageEditor;
using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StageEditorPresenter : MonoBehaviour
{
    [SerializeField] private GameObject _notifyImage; // 通知用画像オブジェクト
    [SerializeField] private TextMeshProUGUI _editModeTextMesh;
    [SerializeField] private TextMeshProUGUI _tileTypeTextMesh;
    [SerializeField] private TextMeshProUGUI _heightTextMesh;

    public void Init()
    {
    }

    /// <summary>
    /// 通知ビューの表示/非表示を切り替えます。
    /// </summary>
    public void ToggleNotifyView()
    {
        _notifyImage.SetActive(!_notifyImage.activeSelf);
    }

    /// <summary>
    /// 通知ビューに表示するテキストを設定します。
    /// </summary>
    /// <param name="word">表示テキスト</param>
    public void SetNotifyWord(string word)
    {
        if (_notifyImage != null)
        {
            _notifyImage.GetComponentInChildren<TextMeshProUGUI>().text = word;
        }
    }

    /// <summary>
    /// エディット可能パラメータのテキストを更新します。
    /// </summary>
    /// <param name="type">タイプ</param>
    /// <param name="height">高さ</param>
    public void UpdateText(StageEditMode mode, int type, float height)
    {
        _editModeTextMesh.text  = mode.ToString().Replace('_', ' ');
        _tileTypeTextMesh.text  = ((TileType)type).ToString();
        _heightTextMesh.text    = height.ToString();
    }
}
