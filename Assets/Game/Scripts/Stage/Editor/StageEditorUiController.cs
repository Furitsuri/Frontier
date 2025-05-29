using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class StageEditorUiController : MonoBehaviour
{
    [SerializeField] private GameObject editorPanel;

    void Start()
    {
#if UNITY_EDITOR
        editorPanel.SetActive(true);
#else
        editorPanel.SetActive(false);
#endif
    }
}