using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageEditorLauncher : MonoBehaviour
{
    [SerializeField]
    private GameObject _stageEditorUI;

    public void LaunchEditor()
    {
        _stageEditorUI.SetActive(true);

        // ゲームロジック停止・入力無効化
        // GameInputController.Instance.enabled = false;
        Time.timeScale = 0f; // 編集中はゲームを止めても良い
    }

    public void ExitEditor()
    {
        _stageEditorUI.SetActive(false);
        // GameInputController.Instance.enabled = true;
        Time.timeScale = 1f;
    }
}