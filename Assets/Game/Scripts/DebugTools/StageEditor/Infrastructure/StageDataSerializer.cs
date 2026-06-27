using Frontier.Stage;
using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// StageDataのシリアライズとデシリアライズを行うクラス
    /// </summary>
    /// <remarks>
    /// StageDataはMonoBehaviourを継承しているため、JsonUtilityで直接シリアライズできない。
    /// そのため、StageDataのデータを保持するクラスを作成し、それをシリアライズする。
    /// </remarks>
    static public class StageDataSerializer
    {
        private static string GetFilePath( string fileName )
        {
            return Path.Combine(
                "Assets", "Resources", "StageData",
                $"{fileName}.json" );
        }

        static public bool Save( StageData data, string fileName )
        {
            try
            {
                data.SetupSaveData(); // 保存用データのセットアップ

                string path = GetFilePath( fileName );
                Directory.CreateDirectory( Path.GetDirectoryName( path ) );
                string json = JsonUtility.ToJson( data, true );
                File.WriteAllText( path, json );

                Debug.Log( json );

                return true; // 成功
            }
            catch( Exception e )
            {
                Debug.LogError( $"ステージデータの保存に失敗しました: {e.Message}" );
                return false; // 失敗
            }
        }


        static public StageData Load( string fileName )
        {
            string path = GetFilePath( fileName );
            if( !File.Exists( path ) ) return null;
            string json = File.ReadAllText( path );
            return JsonUtility.FromJson<StageData>( json );
        }
    }
}

#endif // UNITY_EDITOR