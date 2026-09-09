using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// トークウィンドウのシーンごとの発言データ(Assets/Resources/TalkData/{シーン名}.json)を読み込みます。
    /// 状態を持たないためDIコンテナへのバインドは不要で、どのシーンからでも直接呼び出せます。
    /// </summary>
    public static class TalkWindowDataLoader
    {
        /// <summary>
        /// 指定シーンの発言データをIdをキーにした辞書として読み込みます。
        /// データファイルが存在しない場合は空の辞書を返します。
        /// </summary>
        public static Dictionary<string, TalkEntryData> Load( string sceneName )
        {
            var result = new Dictionary<string, TalkEntryData>();

            var asset = Resources.Load<TextAsset>( $"TalkData/{sceneName}" );
            if ( asset == null )
            {
                Debug.LogWarning( $"[TalkWindowDataLoader] トークデータが見つかりません: Resources/TalkData/{sceneName}.json" );
                return result;
            }

            var sceneData = JsonConvert.DeserializeObject<TalkWindowSceneData>( asset.text );
            if ( sceneData?.Entries == null ) { return result; }

            foreach ( var entry in sceneData.Entries )
            {
                if ( string.IsNullOrEmpty( entry.Id ) ) { continue; }
                result[entry.Id] = entry;
            }

            return result;
        }
    }
}
