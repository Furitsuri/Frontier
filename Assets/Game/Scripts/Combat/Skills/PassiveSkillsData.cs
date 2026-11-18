using System;
using UnityEngine;

namespace Frontier.Combat
{
    /// <summary>
    /// パッシブスキル(装備しているだけで常時効果を持つスキル)のデータです。
    /// ActiveSkillsDataと同様、Resources/SkillData/PassiveSkillData.jsonを最初のアクセス時に一度だけ読み込み、
    /// 以後はシーンを跨いで保持します。
    /// スキル強化に備え、効果量は強化段階(Grade)ごとの配列(Grades)で保持します。Grades[0]が未強化の値です。
    /// </summary>
    static public class PassiveSkillsData
    {
        /// <summary>
        /// 強化段階1つ分のデータです
        /// </summary>
        [Serializable]
        public struct GradeData
        {
            public float Value;     // 効果量(意味はEffectTypeによる)
        }

        public struct Data
        {
            public string Name;                 // スキル名
            public PassiveEffectType EffectType; // 効果の種類
            public GradeData[] Grades;          // 強化段階ごとのデータ(Grades[0]が未強化)
            public LocKey ExplainTextKey;       // スキル説明文のテキストキー
        }

        // JsonUtility.FromJson()によるJSONデシリアライズでのみ値が設定されるフィールドのため、
        // C#コード上では代入箇所が無くCS0649(未割り当て)警告が出る。意図的なものとして抑制する。
#pragma warning disable 0649
        [Serializable]
        private struct FileData
        {
            public string Name;
            public int EffectType;
            public GradeData[] Grades;
            public string ExplainTextKey;
        }

        [Serializable]
        private class PassiveSkillDataContainer
        {
            public FileData[] PassiveSkillsData;
        }
#pragma warning restore 0649

        private const string ResourcesPath = "SkillData/PassiveSkillData";

        static public Data[] data = new Data[( int ) PassiveSkillID.NUM];

        static PassiveSkillsData()
        {
            Load();
        }

        static private void Load()
        {
            var asset = Resources.Load<TextAsset>( ResourcesPath );
            if ( asset == null )
            {
                Debug.LogWarning( $"[PassiveSkillsData] スキルデータが見つかりません: Resources/{ResourcesPath}.json" );
                return;
            }

            var container = JsonUtility.FromJson<PassiveSkillDataContainer>( asset.text );
            if ( container == null || container.PassiveSkillsData == null )
            {
                Debug.LogWarning( "[PassiveSkillsData] スキルデータの読み込みに失敗しました" );
                return;
            }

            int count = Mathf.Min( container.PassiveSkillsData.Length, data.Length );
            for ( int i = 0; i < count; ++i )
            {
                var fdata = container.PassiveSkillsData[i];
                data[i].Name            = fdata.Name;
                data[i].EffectType      = ( PassiveEffectType ) fdata.EffectType;
                data[i].Grades          = fdata.Grades ?? Array.Empty<GradeData>();
                data[i].ExplainTextKey  = ParseExplainTextKey( fdata.ExplainTextKey );

                if ( data[i].Grades.Length == 0 )
                {
                    Debug.LogWarning( $"[PassiveSkillsData] 強化段階のデータがありません: {fdata.Name}" );
                }
            }
        }

        static private LocKey ParseExplainTextKey( string keyName )
        {
            if ( string.IsNullOrEmpty( keyName ) ) { return LocKey.None; }

            if ( Enum.TryParse<LocKey>( keyName, out var key ) ) { return key; }

            Debug.LogWarning( $"[PassiveSkillsData] ExplainTextKeyが不正です: {keyName}" );
            return LocKey.None;
        }

        static public bool IsValidSkill( PassiveSkillID skillID )
        {
            return PassiveSkillID.NONE < skillID && skillID < PassiveSkillID.NUM;
        }

        /// <summary>
        /// 装備中のパッシブスキルのうち、指定の効果を持つものの効果量を合計します。
        /// 戦闘中の能力補正など、パッシブスキルの効果を数値として適用する際に用います。
        /// TODO : スキル強化の実装時に、装備枠ごとの強化段階を受け取り、その段階の効果量を用いるよう変更すること(現状は全て未強化として扱う)
        /// </summary>
        /// <param name="equippedSkills">装備中のパッシブスキル(Status.PassiveSkills)。nullの場合は0を返します</param>
        /// <param name="effectType">合計する効果の種類</param>
        static public float SumEffectValue( PassiveSkillID[] equippedSkills, PassiveEffectType effectType )
        {
            if ( equippedSkills == null ) { return 0f; }

            float total = 0f;
            foreach ( var skillID in equippedSkills )
            {
                if ( !IsValidSkill( skillID ) ) { continue; }
                if ( data[( int ) skillID].EffectType != effectType ) { continue; }

                total += GetGradeData( skillID, 0 ).Value;
            }

            return total;
        }

        /// <summary>
        /// 指定スキルの指定強化段階のデータを取得します。段階がデータの範囲を超える場合は、範囲内に丸めた段階のデータを返します。
        /// </summary>
        static public GradeData GetGradeData( PassiveSkillID skillID, int grade )
        {
            if ( !IsValidSkill( skillID ) ) { return default; }

            var grades = data[( int ) skillID].Grades;
            if ( grades == null || grades.Length == 0 ) { return default; }

            return grades[Mathf.Clamp( grade, 0, grades.Length - 1 )];
        }
    }
}
