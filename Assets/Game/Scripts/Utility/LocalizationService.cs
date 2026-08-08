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

    private Dictionary<LocKey, string> LoadTable( Language lang )
    {
        var path  = $"Localization/{lang}";
        var asset = Resources.Load<TextAsset>( path );
        if ( asset == null )
        {
            Debug.LogWarning( $"[Localization] ローカライズデータが見つかりません: Resources/{path}.json" );
            return new Dictionary<LocKey, string>();
        }

        var table = JsonConvert.DeserializeObject<Dictionary<LocKey, string>>( asset.text );
        return table ?? new Dictionary<LocKey, string>();
    }
}
