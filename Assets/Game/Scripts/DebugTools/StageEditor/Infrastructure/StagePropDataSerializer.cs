using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// ステージエディターで配置した StageProp データの保存・読込を行うクラス。
    /// 保存先: Assets/Resources/CharactersData/{fileName}/Frontier_{fileName}_StagePropData.json
    /// </summary>
    static public class StagePropDataSerializer
    {
        [System.Serializable]
        public class StagePropStatusData
        {
            public int Prefab;
            public int TileIndex;
            public int Direction;
        }

        [System.Serializable]
        private class SaveContainer
        {
            public StagePropStatusData[] StagePropStatuses;
        }

        private static string GetFilePath( string fileName )
        {
            return Path.Combine(
                Application.dataPath,
                "Resources", "CharactersData", fileName,
                $"Frontier_{fileName}_StagePropData.json" );
        }

        static public bool Save( List<StagePropStatusData> propList, string fileName )
        {
            string path = GetFilePath( fileName );
            string dir  = Path.GetDirectoryName( path );

            try
            {
                Directory.CreateDirectory( dir );
            }
            catch ( Exception e )
            {
                Debug.LogError( $"[StagePropDataSerializer] フォルダの作成に失敗しました: {e.Message}" );
                return false;
            }

            try
            {
                var container = new SaveContainer { StagePropStatuses = propList.ToArray() };
                string json   = JsonUtility.ToJson( container, true );
                File.WriteAllText( path, json );
                Debug.Log( $"[StagePropDataSerializer] ステージプロップデータを保存しました ({propList.Count} 個): {path}" );
                return true;
            }
            catch ( Exception e )
            {
                Debug.LogError( $"[StagePropDataSerializer] 保存に失敗しました: {e.Message}" );
                return false;
            }
        }

        static public List<StagePropStatusData> Load( string fileName )
        {
            string path = GetFilePath( fileName );

            if ( !File.Exists( path ) )
            {
                Debug.Log( $"[StagePropDataSerializer] ステージプロップデータファイルが見つかりません: {path}" );
                return null;
            }

            try
            {
                string json      = File.ReadAllText( path );
                var    container = JsonUtility.FromJson<SaveContainer>( json );
                if ( container == null || container.StagePropStatuses == null ) return null;
                Debug.Log( $"[StagePropDataSerializer] ステージプロップデータを読み込みました ({container.StagePropStatuses.Length} 個): {path}" );
                return new List<StagePropStatusData>( container.StagePropStatuses );
            }
            catch ( Exception e )
            {
                Debug.LogError( $"[StagePropDataSerializer] 読込に失敗しました: {e.Message}" );
                return null;
            }
        }
    }
}

#endif // UNITY_EDITOR
