using Frontier.Loaders;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// ステージエディターで配置した敵キャラクターデータの保存・読込を行うクラス。
    /// 保存先: Assets/Resources/CharactersData/{fileName}/Frontier_{fileName}_CharacterData_Enemy.json
    /// フォルダが存在しない場合は保存をスキップします。
    /// </summary>
    static public class EnemyDataSerializer
    {
        [System.Serializable]
        private class SaveContainer
        {
            public BattleFileLoader.CharacterDeployData[] CharacterStatuses;
        }

        private static string GetFilePath( string fileName )
        {
            return Path.Combine(
                Application.dataPath,
                "Resources", "CharactersData", fileName,
                $"Frontier_{fileName}_CharacterData_Enemy.json" );
        }

        /// <summary>
        /// 敵ステータスリストを JSON として保存します。
        /// フォルダが存在しない場合は警告を出して false を返します。
        /// </summary>
        static public bool Save( List<BattleFileLoader.CharacterDeployData> enemyList, string fileName )
        {
            string path = GetFilePath( fileName );
            string dir  = Path.GetDirectoryName( path );

            try
            {
                Directory.CreateDirectory( dir );
            }
            catch ( Exception e )
            {
                Debug.LogError( $"[EnemyDataSerializer] フォルダの作成に失敗しました: {e.Message}" );
                return false;
            }

            try
            {
                // EquipSkills が null のエントリを補完してからシリアライズ
                var entries = enemyList.ToArray();
                for ( int i = 0; i < entries.Length; i++ )
                {
                    if ( entries[i].status.EquipSkills == null )
                    {
                        var e = entries[i];
                        e.status.EquipSkills = new Frontier.Combat.SkillID[] { Frontier.Combat.SkillID.NONE, Frontier.Combat.SkillID.NONE, Frontier.Combat.SkillID.NONE, Frontier.Combat.SkillID.NONE };
                        entries[i] = e;
                    }
                }

                var container = new SaveContainer { CharacterStatuses = entries };
                string json   = JsonUtility.ToJson( container, true );
                File.WriteAllText( path, json );

                Debug.Log( $"[EnemyDataSerializer] 敵データを保存しました ({entries.Length} 体): {path}" );
                return true;
            }
            catch ( Exception e )
            {
                Debug.LogError( $"[EnemyDataSerializer] 保存に失敗しました: {e.Message}" );
                return false;
            }
        }

        /// <summary>
        /// JSON から敵ステータスリストを読み込みます。
        /// ファイルが存在しない場合は null を返します。
        /// </summary>
        static public List<BattleFileLoader.CharacterDeployData> Load( string fileName )
        {
            string path = GetFilePath( fileName );

            if ( !File.Exists( path ) )
            {
                Debug.Log( $"[EnemyDataSerializer] 敵データファイルが見つかりません: {path}" );
                return null;
            }

            try
            {
                string json       = File.ReadAllText( path );
                var    container  = JsonUtility.FromJson<SaveContainer>( json );
                if ( container == null || container.CharacterStatuses == null ) return null;

                Debug.Log( $"[EnemyDataSerializer] 敵データを読み込みました ({container.CharacterStatuses.Length} 体): {path}" );
                return new List<BattleFileLoader.CharacterDeployData>( container.CharacterStatuses );
            }
            catch ( Exception e )
            {
                Debug.LogError( $"[EnemyDataSerializer] 読込に失敗しました: {e.Message}" );
                return null;
            }
        }
    }
}

#endif // UNITY_EDITOR
