using System;
using System.IO;
using UnityEngine;

namespace Frontier.Field
{
    /// <summary>
    /// FieldData の保存・読込を行うクラス。
    /// 読込: Assets/Resources/FieldData/{fieldId}.json (Resources.Load)
    /// 保存 (Editorのみ): Assets/Resources/FieldData/{fieldId}.json
    /// </summary>
    public static class FieldDataSerializer
    {
        private const string ResourcesFolder = "FieldData";

#if UNITY_EDITOR
        private static string GetEditorFilePath( string fieldId )
        {
            return Path.Combine(
                Application.dataPath,
                "Resources", ResourcesFolder,
                $"{fieldId}.json" );
        }

        public static bool Save( FieldData data )
        {
            string path = GetEditorFilePath( data.FieldId );
            string dir  = Path.GetDirectoryName( path );

            try
            {
                Directory.CreateDirectory( dir );
            }
            catch ( Exception e )
            {
                Debug.LogError( $"[FieldDataSerializer] フォルダの作成に失敗しました: {e.Message}" );
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson( data, true );
                File.WriteAllText( path, json );
                Debug.Log( $"[FieldDataSerializer] フィールドデータを保存しました: {path}" );
                return true;
            }
            catch ( Exception e )
            {
                Debug.LogError( $"[FieldDataSerializer] 保存に失敗しました: {e.Message}" );
                return false;
            }
        }
#endif // UNITY_EDITOR

        public static FieldData Load( string fieldId )
        {
            var asset = Resources.Load<TextAsset>( $"{ResourcesFolder}/{fieldId}" );
            if ( asset == null )
            {
                Debug.LogWarning( $"[FieldDataSerializer] フィールドデータが見つかりません: Resources/{ResourcesFolder}/{fieldId}.json" );
                return null;
            }

            try
            {
                FieldData data = JsonUtility.FromJson<FieldData>( asset.text );
                Debug.Log( $"[FieldDataSerializer] フィールドデータを読み込みました: {fieldId}" );
                return data;
            }
            catch ( Exception e )
            {
                Debug.LogError( $"[FieldDataSerializer] 読込に失敗しました: {e.Message}" );
                return null;
            }
        }
    }
}
