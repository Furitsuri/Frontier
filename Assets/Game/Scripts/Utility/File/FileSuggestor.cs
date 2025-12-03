using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FileSuggestor : MonoBehaviour
{
    [SerializeField] private TMP_InputField fileInputField;
    [SerializeField] private Transform suggestionParent;  // ScrollView Content
    [SerializeField] private GameObject suggestionItemPrefab;
    [SerializeField] private string targetDirectory = "C:/Users/tssm4/AppData/LocalLow/DefaultCompany/FRONTIER/StageData";

    private int _defaultChildCount = 0;
    private Action OnPressedTabAction = null;

    private void Awake()
    {
        _defaultChildCount = suggestionParent.childCount;
    }

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
        gameObject.SetActive( true );

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
        GameObject[] textMeshChilds = GetTextMeshChildArray();

        if( textMeshChilds.Length == 0 ) { return null; }
        return textMeshChilds[0];
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
            string fileName = Path.GetFileName( path );

            // 入力に合致したファイルのみ表示
            if( !fileName.ToLower().Contains( text.ToLower() ) ) { continue; }

            var item = Instantiate( suggestionItemPrefab, suggestionParent );
            item.gameObject.SetActive( true );
            var tmp = item.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = fileName;
        }
    }

    private void DestroyTextMeshChild()
    {
        foreach( Transform child in suggestionParent )
        {
            if( null != child.GetComponentInChildren<TextMeshProUGUI>() )
            {
                Destroy( child.gameObject );
            }
        }
    }

    private GameObject[] GetTextMeshChildArray()
    {
        var list = new System.Collections.Generic.List<GameObject>();
        foreach( Transform child in suggestionParent )
        {
            if( null != child.GetComponentInChildren<TextMeshProUGUI>() )
            {
                list.Add( child.gameObject );
            }
        }
        return list.ToArray();
    }
}
