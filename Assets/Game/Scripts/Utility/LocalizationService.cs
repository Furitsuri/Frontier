using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class LocalizationService : ILocalizationService
{
    // Get()でキーが見つからない場合に最終的にフォールバックする基準言語。
    // プロジェクトの一次データが日本語であるため日本語を基準とする。
    private const Language BaseLanguage = Language.Japanese;

    private readonly Dictionary<Language, Dictionary<LocKey, string>> _tables = new();

    public Language CurrentLanguage { get; private set; } = Language.English;

    public event Action OnLanguageChanged;

    public LocalizationService()
    {
        GetOrLoadTable( CurrentLanguage );
    }

    public void ChangeLanguage( Language lang )
    {
        if ( lang == CurrentLanguage ) return;

        CurrentLanguage = lang;
        GetOrLoadTable( lang );
        OnLanguageChanged?.Invoke();
    }

    public string Get( LocKey key )
    {
        var currentTable = GetOrLoadTable( CurrentLanguage );
        if ( currentTable.TryGetValue( key, out var value ) )
        {
            return value;
        }

        if ( CurrentLanguage != BaseLanguage )
        {
            var baseTable = GetOrLoadTable( BaseLanguage );
            if ( baseTable.TryGetValue( key, out var baseValue ) )
            {
                Debug.LogWarning( $"[Localization] key not found: {key} ({CurrentLanguage}). {BaseLanguage}の文言で代替しました。" );
                return baseValue;
            }
        }

        Debug.LogWarning( $"[Localization] key not found: {key} ({CurrentLanguage}, {BaseLanguage})" );
        return key.ToString();
    }

    private Dictionary<LocKey, string> GetOrLoadTable( Language lang )
    {
        if ( _tables.TryGetValue( lang, out var table ) ) return table;

        table = LoadTable( lang );
        _tables[lang] = table;
        return table;
    }

    /// <summary>
    /// 指定言語のローカライズデータを読み込みます。
    /// 文言は Resources/Localization/{言語名}/ 以下に、内容に応じて複数のJSONファイル(UI.json、Talk.json等)へ分けて定義されており、
    /// そのフォルダ内の全てのファイルを読み込んで1つのテーブルへ統合します。
    /// ファイルの分け方は読み込みに影響しないため、ファイルの追加や、ファイル間でのキーの移動は自由に行えます
    /// (ただし、同じキーを複数のファイルに定義することは出来ません)。
    /// </summary>
    private Dictionary<LocKey, string> LoadTable( Language lang )
    {
        var table  = new Dictionary<LocKey, string>();
        var path   = $"Localization/{lang}";
        var assets = Resources.LoadAll<TextAsset>( path );
        if ( assets == null || assets.Length <= 0 )
        {
            Debug.LogWarning( $"[Localization] ローカライズデータが見つかりません: Resources/{path}/" );
            return table;
        }

        // どのファイルで定義されたキーかを記録し、重複を検出した際に双方のファイル名を示せるようにする
        var definedFileNames = new Dictionary<LocKey, string>();

        foreach ( var asset in assets )
        {
            var fileTable = JsonConvert.DeserializeObject<Dictionary<LocKey, string>>( asset.text );
            if ( fileTable == null ) continue;

            foreach ( var pair in fileTable )
            {
                if ( definedFileNames.TryGetValue( pair.Key, out var definedFileName ) )
                {
                    Debug.LogError( $"[Localization] キーが重複しています: {pair.Key} ({lang}/{definedFileName}.json と {lang}/{asset.name}.json)。先に読み込んだ側の文言を使用します。" );
                    continue;
                }

                table[pair.Key]            = pair.Value;
                definedFileNames[pair.Key] = asset.name;
            }
        }

        return table;
    }
}
