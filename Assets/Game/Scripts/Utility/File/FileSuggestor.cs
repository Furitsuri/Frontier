using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent( typeof( Image ) )]
[RequireComponent( typeof( ScrollRect ) )]
public class FileSuggestor : MonoBehaviour
{
    [SerializeField] private TMP_InputField fileInputField;
    [SerializeField] private GameObject suggestionItemPrefab;
    [SerializeField] private string targetDirectory = "C:/Users/tssm4/AppData/LocalLow/DefaultCompany/FRONTIER/StageData";

    private GameObject[] _textMeshChilds = null;
    private Action OnPressedTabAction = null;

    void Update()
    {
        if( Input.GetKeyDown( KeyCode.Tab ) )
        {
            OnPressedTabAction?.Invoke();
        }
    }

    public void Init( Action onPressedTab )
    {
        OnPressedTabAction = onPressedTab;

        gameObject.SetActive( false );
    }

    public void StartSuggest()
    {
        _textMeshChilds = GetTextMeshChildArray();

        fileInputField.onValueChanged.AddListener( OnTextChanged );
    }

    public void EndSuggest()
    {
        DestroyTextMeshChild();

        gameObject.SetActive( false );
    }

    /// <summary>
    /// 候補の最上位のGameObjectを取得します
    /// </summary>
    /// <returns></returns>
    public GameObject GetTopMostSuggestion()
    {
        if( _textMeshChilds.Length == 0 ) { return null; }
        return _textMeshChilds[0];
    }

    private void OnTextChanged( string text )
    {
        // 既存の候補をクリア
        DestroyTextMeshChild();

        if( string.IsNullOrEmpty( text ) ) { return; }
        if( !Directory.Exists( targetDirectory ) ) { return; }

        var files = Directory.GetFiles( targetDirectory );

        foreach( var path in files )
        {
            string fileName = Path.GetFileNameWithoutExtension( path );

            // 入力に合致したファイルのみ表示
            if( !fileName.ToLower().Contains( text.ToLower() ) ) { continue; }

            var item = Instantiate( suggestionItemPrefab, this.transform );
            item.gameObject.SetActive( true );
            var tmp = item.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = fileName;
        }

        gameObject.SetActive( true );
        _textMeshChilds = GetTextMeshChildArray();  // 子の配列を更新
    }

    private void DestroyTextMeshChild()
    {
        foreach( Transform child in this.transform )
        {
            if( null != child.GetComponentInChildren<TextMeshProUGUI>() )
            {
                child.transform.SetParent( null );  // Destroyは即時に実行されないため、親子関係を明示的にここで切り離す
                Destroy( child.gameObject );
            }
        }
    }

    private GameObject[] GetTextMeshChildArray()
    {
        var list = new System.Collections.Generic.List<GameObject>();
        foreach( Transform child in this.transform )
        {
            if( null != child.GetComponentInChildren<TextMeshProUGUI>() )
            {
                list.Add( child.gameObject );
            }
        }
        return list.ToArray();
    }
}
