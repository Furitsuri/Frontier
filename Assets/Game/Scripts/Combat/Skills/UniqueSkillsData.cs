using Frontier.Entities;
using System;
using UnityEngine;

namespace Frontier.Combat
{
    /// <summary>
    /// キャラクターの種別ごとの固有スキルのデータです。スキルの定義と、種別との対応表を持ちます。
    /// Resources/SkillData/UniqueSkillData.jsonを最初のアクセス時に一度だけ読み込みます(PassiveSkillsData等と同じstatic自己ロード)。
    /// 固有スキルは性質としては常時効果ですが、パッシブスキル(PassiveSkillsData)とは独立したスキルとして扱い、効果の表現(PassiveEffect)のみを共有します。
    /// MEMO : 固有スキルとその強化段階は、キャラクターの種別(Status.characterTag + Status.PrefabIndex)とレベルから毎回求め、Statusには保持しません。
    ///        そのため装備の変更は構造上出来ず、セーブデータにも含まれません。
    /// </summary>
    static public class UniqueSkillsData
    {
        public struct Data
        {
            public string Name;                 // スキル名
            public PassiveEffect Effect;        // 効果(種類と強化段階ごとの効果量)
            public LocKey ExplainTextKey;       // スキル説明文のテキストキー
        }

        // JsonUtility.FromJson()によるJSONデシリアライズでのみ値が設定されるフィールドのため、CS0649(未割り当て)警告を抑制する。
#pragma warning disable 0649
        [Serializable]
        private struct SkillFileData
        {
            public string Name;
            public int EffectType;
            public PassiveEffect.GradeData[] Grades;
            public string ExplainTextKey;
        }

        [Serializable]
        private class FileData
        {
            public int[] GradeUpLevels;                 // 強化段階が上がるレベル(全種別共通。昇順。要素数が強化段階の上限数)
            public SkillFileData[] UniqueSkillsData;    // 固有スキルの定義(UniqueSkillIDの並び順)
            public string[] PlayerSkills;               // プレイヤーの種別(PrefabRegistryのPlayerPrefabsの並び順)ごとの固有スキル名(UniqueSkillID)
            public string[] EnemySkills;                // 敵の種別(PrefabRegistryのEnemyPrefabsの並び順)ごとの固有スキル名(UniqueSkillID)
        }
#pragma warning restore 0649

        private const string ResourcesPath = "SkillData/UniqueSkillData";

        static public Data[] data = new Data[( int ) UniqueSkillID.NUM];

        static private int[] _gradeUpLevels             = Array.Empty<int>();
        static private UniqueSkillID[] _playerSkills    = Array.Empty<UniqueSkillID>();
        static private UniqueSkillID[] _enemySkills     = Array.Empty<UniqueSkillID>();

        static UniqueSkillsData()
        {
            Load();
        }

        static public bool IsValidSkill( UniqueSkillID skillID )
        {
            return UniqueSkillID.NONE < skillID && skillID < UniqueSkillID.NUM;
        }

        /// <summary>
        /// 指定キャラクターの固有スキルを取得します。固有スキルが設定されていない種別の場合はNONEを返します。
        /// </summary>
        static public UniqueSkillID GetSkillID( in Status status )
        {
            return GetSkillID( status.characterTag, status.PrefabIndex );
        }

        /// <summary>
        /// 指定種別の固有スキルを取得します。固有スキルが設定されていない種別の場合はNONEを返します。
        /// </summary>
        static public UniqueSkillID GetSkillID( CHARACTER_TAG tag, int prefabIndex )
        {
            UniqueSkillID[] table;
            switch ( tag )
            {
                case CHARACTER_TAG.PLAYER:  table = _playerSkills; break;
                case CHARACTER_TAG.ENEMY:   table = _enemySkills; break;
                default:                    return UniqueSkillID.NONE;
            }

            if ( prefabIndex < 0 || table.Length <= prefabIndex ) { return UniqueSkillID.NONE; }

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

            LoadSkills( fdata.UniqueSkillsData );
            _gradeUpLevels  = fdata.GradeUpLevels ?? Array.Empty<int>();
            _playerSkills   = ParseSkillIDs( fdata.PlayerSkills );
            _enemySkills    = ParseSkillIDs( fdata.EnemySkills );
        }

        static private void LoadSkills( SkillFileData[] skills )
        {
            if ( skills == null )
            {
                Debug.LogWarning( "[UniqueSkillsData] 固有スキルの定義がありません" );
                return;
            }

            int count = Mathf.Min( skills.Length, data.Length );
            for ( int i = 0; i < count; ++i )
            {
                var fdata = skills[i];
                data[i].Name            = fdata.Name;
                data[i].Effect          = new PassiveEffect( ( PassiveEffectType ) fdata.EffectType, fdata.Grades );
                data[i].ExplainTextKey  = ParseExplainTextKey( fdata.ExplainTextKey );

                if ( data[i].Effect.Grades.Length == 0 )
                {
                    Debug.LogWarning( $"[UniqueSkillsData] 強化段階のデータがありません: {fdata.Name}" );
                }
            }
        }

        static private LocKey ParseExplainTextKey( string keyName )
        {
            if ( string.IsNullOrEmpty( keyName ) ) { return LocKey.None; }

            if ( Enum.TryParse<LocKey>( keyName, out var key ) ) { return key; }

            Debug.LogWarning( $"[UniqueSkillsData] ExplainTextKeyが不正です: {keyName}" );
            return LocKey.None;
        }

        static private UniqueSkillID[] ParseSkillIDs( string[] names )
        {
            if ( names == null ) { return Array.Empty<UniqueSkillID>(); }

            var skills = new UniqueSkillID[names.Length];
            for ( int i = 0; i < names.Length; ++i )
            {
                skills[i] = UniqueSkillID.NONE;
                if ( string.IsNullOrEmpty( names[i] ) ) { continue; }

                if ( !Enum.TryParse<UniqueSkillID>( names[i], out var skillID ) || !IsValidSkill( skillID ) )
                {
                    Debug.LogWarning( $"[UniqueSkillsData] スキル名が不正です: {names[i]}" );
                    continue;
                }

                skills[i] = skillID;
            }

            return skills;
        }
    }
}
