using System;
using UnityEngine;

namespace Frontier.Combat
{
    /// <summary>
    /// パッシブスキル(装備しているだけで常時効果を持つスキル)のデータです。
    /// ActiveSkillsDataと同様、Resources/SkillData/PassiveSkillData.jsonを最初のアクセス時に一度だけ読み込み、
    /// 以後はシーンを跨いで保持します。
    /// スキル強化に備え、効果量は強化段階(Grade)ごとの配列(Grades)で保持します。Grades[0]が未強化の値です。
    /// MEMO : キャラクターの種別ごとの固有スキルは、性質は同じ常時効果ですが別物として扱います(UniqueSkillsData参照)。
    ///        効果の表現(PassiveEffect)のみを共有しています。
    /// </summary>
    static public class PassiveSkillsData
    {
        public struct Data
        {
            public string Name;                 // スキル名
            public PassiveEffect Effect;        // 効果(種類と強化段階ごとの効果量)
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
            public PassiveEffect.GradeData[] Grades;
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
                data[i].Effect          = new PassiveEffect( ( PassiveEffectType ) fdata.EffectType, fdata.Grades );
                data[i].ExplainTextKey  = ParseExplainTextKey( fdata.ExplainTextKey );

                if ( data[i].Effect.Grades.Length == 0 )
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
    }
}
