using System.IO;
using UnityEngine;

namespace Frontier.Option
{
    /// <summary>
    /// オプション設定(音量・入力ガイド表示等)の保存/読込を行います。
    /// UserDomain(プレイ内容のセーブデータ)とは独立して永続化します。
    /// </summary>
    public class OptionSaveHandler : ISaveHandler<OptionSaveData>
    {
        private readonly string _filePath;

        public OptionSaveHandler()
        {
            _filePath = Path.Combine( Application.persistentDataPath, "option.json" );
        }

        public void Save( OptionSaveData data )
        {
            string json = JsonUtility.ToJson( data );
            File.WriteAllText( _filePath, json );
        }

        public OptionSaveData Load()
        {
            if( !File.Exists( _filePath ) )
                return new OptionSaveData();

            string json = File.ReadAllText( _filePath );
            return JsonUtility.FromJson<OptionSaveData>( json );
        }
    }
}
