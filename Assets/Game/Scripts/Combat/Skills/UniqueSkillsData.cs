using Frontier.Entities;
using System;
using UnityEngine;

namespace Frontier.Combat
{
    /// <summary>
    /// キャラクターの種別ごとの固有スキルの対応表です。固有スキルは必ずパッシブスキルであり、効果はPassiveSkillsData(IsUnique=true)に定義します。
    /// Resources/SkillData/UniqueSkillData.jsonを最初のアクセス時に一度だけ読み込みます(ActiveSkillsData等と同じstatic自己ロード)。
    /// MEMO : 固有スキルとその強化段階は、キャラクターの種別(Status.characterTag + Status.PrefabIndex)とレベルから毎回求め、Statusには保持しません。
    ///        そのため装備の変更は構造上出来ず、セーブデータにも含まれません。
    /// </summary>
    static public class UniqueSkillsData
    {
        // JsonUtility.FromJson()によるJSONデシリアライズでのみ値が設定されるフィールドのため、CS0649(未割り当て)警告を抑制する。
#pragma warning disable 0649
        [Serializable]
        private class FileData
        {
            public int[] GradeUpLevels;     // 強化段階が上がるレベル(全種別共通。昇順。要素数が強化段階の上限数)
            public string[] PlayerSkills;   // プレイヤーの種別(PrefabRegistryのPlayerPrefabsの並び順)ごとの固有スキル名(PassiveSkillID)
            public string[] EnemySkills;    // 敵の種別(PrefabRegistryのEnemyPrefabsの並び順)ごとの固有スキル名(PassiveSkillID)
        }
#pragma warning restore 0649

        private const string ResourcesPath = "SkillData/UniqueSkillData";

        static private int[] _gradeUpLevels             = Array.Empty<int>();
        static private PassiveSkillID[] _playerSkills   = Array.Empty<PassiveSkillID>();
        static private PassiveSkillID[] _enemySkills    = Array.Empty<PassiveSkillID>();

        static UniqueSkillsData()
        {
            Load();
        }

        /// <summary>
        /// 指定キャラクターの固有スキルを取得します。固有スキルが設定されていない種別の場合はNONEを返します。
        /// </summary>
        static public PassiveSkillID GetSkillID( in Status status )
        {
            return GetSkillID( status.characterTag, status.PrefabIndex );
        }

        /// <summary>
        /// 指定種別の固有スキルを取得します。固有スキルが設定されていない種別の場合はNONEを返します。
        /// </summary>
        static public PassiveSkillID GetSkillID( CHARACTER_TAG tag, int prefabIndex )
        {
            PassiveSkillID[] table;
            switch ( tag )
            {
                case CHARACTER_TAG.PLAYER:  table = _playerSkills; break;
                case CHARACTER_TAG.ENEMY:   table = _enemySkills; break;
                default:                    return PassiveSkillID.NONE;
            }

            if ( prefabIndex < 0 || table.Length <= prefabIndex ) { return PassiveSkillID.NONE; }

            return table[prefabIndex];
        }

        /// <summary>
        /// 指定レベルでの固有スキルの強化段階(0が未強化)を取得します。全種別共通です。
        /// </summary>
        static public int GetGrade( int level )
        {
            int grade = 0;
            foreach ( var gradeUpLevel in _gradeUpLevels )
            {
                if ( gradeUpLevel <= level ) { ++grade; }
            }

            return grade;
        }

        static private void Load()
        {
            var asset = Resources.Load<TextAsset>( ResourcesPath );
            if ( asset == null )
            {
                Debug.LogWarning( $"[UniqueSkillsData] 固有スキルデータが見つかりません: Resources/{ResourcesPath}.json" );
                return;
            }

            var fdata = JsonUtility.FromJson<FileData>( asset.text );
            if ( fdata == null )
            {
                Debug.LogWarning( "[UniqueSkillsData] 固有スキルデータの読み込みに失敗しました" );
                return;
            }

            _gradeUpLevels  = fdata.GradeUpLevels ?? Array.Empty<int>();
            _playerSkills   = ParseSkills( fdata.PlayerSkills );
            _enemySkills    = ParseSkills( fdata.EnemySkills );
        }

        static private PassiveSkillID[] ParseSkills( string[] names )
        {
            if ( names == null ) { return Array.Empty<PassiveSkillID>(); }

            var skills = new PassiveSkillID[names.Length];
            for ( int i = 0; i < names.Length; ++i )
            {
                skills[i] = PassiveSkillID.NONE;
                if ( string.IsNullOrEmpty( names[i] ) ) { continue; }

                if ( !Enum.TryParse<PassiveSkillID>( names[i], out var skillID ) || !PassiveSkillsData.IsValidSkill( skillID ) )
                {
                    Debug.LogWarning( $"[UniqueSkillsData] スキル名が不正です: {names[i]}" );
                    continue;
                }
                if ( !PassiveSkillsData.IsUniqueSkill( skillID ) )
                {
                    Debug.LogWarning( $"[UniqueSkillsData] 固有スキルではないスキルが指定されています(PassiveSkillData.jsonでIsUniqueをtrueにしてください): {names[i]}" );
                    continue;
                }

                skills[i] = skillID;
            }

            return skills;
        }
    }
}
