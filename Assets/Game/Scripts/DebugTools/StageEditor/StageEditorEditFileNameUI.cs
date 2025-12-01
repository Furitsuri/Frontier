using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StageEditorEditFileNameUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;

    public void Init()
    {
        gameObject.SetActive( false );
    }

    public void Open( System.Action< string > onComplete )
    {
        gameObject.SetActive( true );
        inputField.text = "";

        StartCoroutine( FocusInputField() );

        inputField.onEndEdit.RemoveAllListeners();
        inputField.onEndEdit.AddListener( ( text ) =>
        {
            onComplete?.Invoke( text );
            gameObject.SetActive( false );
        } );
    }

    public bool IsInputFieldFocused()
    {
        return inputField.isFocused;
    }

    private IEnumerator FocusInputField()
    {
        yield return null; // 次のフレームまで待つ( gameObject.SetActive(true) を実行してすぐに Select() を呼んでもUI がまだ有効状態になっていない )
        inputField.Select();
        inputField.ActivateInputField();
        EventSystem.current.SetSelectedGameObject( inputField.gameObject );
    }
}