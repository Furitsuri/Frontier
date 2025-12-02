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

    private void Start()
    {
        // fileInputField.onValueChanged.AddListener( OnTextChanged );
    }

    public void StartSuggest()
    {
        fileInputField.onValueChanged.AddListener( OnTextChanged );
    }

    private void OnTextChanged( string text )
    {
        // 既存の候補をクリア
        foreach( Transform child in suggestionParent )
        {
            Destroy( child.gameObject );
        }

        if( string.IsNullOrEmpty( text ) ) return;
        if( !Directory.Exists( targetDirectory ) ) return;

        var files = Directory.GetFiles( targetDirectory );

        foreach( var path in files )
        {
            string fileName = Path.GetFileName( path );

            // 入力に合致したファイルのみ表示
            if( !fileName.ToLower().Contains( text.ToLower() ) ) continue;

            var item = Instantiate( suggestionItemPrefab, suggestionParent );
            var tmp = item.GetComponentInChildren<Text>();
            tmp.text = fileName;

            // 候補をクリック → InputField に反映
            var button = item.GetComponent<Button>();
            button.onClick.AddListener( () =>
            {
                fileInputField.text = fileName;
            } );
        }
    }
}
