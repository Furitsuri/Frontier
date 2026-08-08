using Frontier.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Combat
{
    /// <summary>
    /// 各スキルの実行内容の関数集合です
    /// </summary>
    static public class SkillsData
    {
        [System.Serializable]
        public struct Data
        {
            public string Name;                     // スキル名
            public int Cost;                        // 使用コスト
            public SituationType SituationType;     // 使用シチュエーションタイプ
            public ActionType ActionType;           // スキルタイプ
            public SkillBitFlag Flags;              // スキルフラグ
            public int Duration;                    // 効果時間（ターン数）
            public RangeShape RangeShape;           // レンジタイプ
            public int RangeValue;                  // レンジ範囲
            public bool IsAdjustableRange;          // レンジ調節可否
            public TargetingMode TargetingMode;     // レンジ内選択モード
            public int TargetingRange;              // レンジ内選択モードに用いる範囲値
            public bool IsMovingSkill;              // 移動を伴うスキルか否か
            public bool IsCooperative;              // 連携コマンドとして使用可能か
            public float AddAtkMag;                 // 攻撃力倍率加算値
            public float AddDefMag;                 // 防御力倍率加算値
            public int AddAtkNum;                   // 攻撃回数加算値
            public float Param1;                    // 汎用パラメータ1
            public float Param2;                    // 汎用パラメータ2
            public float Param3;                    // 汎用パラメータ3
            public float Param4;                    // 汎用パラメータ4
            public LocKey ExplainTextKey;           // スキル説明文のテキストキー
        }

        // JsonUtility.FromJson()によるJSONデシリアライズでのみ値が設定されるフィールドのため、
        // C#コード上では代入箇所が無くCS0649(未割り当て)警告が出る。意図的なものとして抑制する。
#pragma warning disable 0649
        [System.Serializable]
        private struct FileData
        {
            public string Name;
            public int Cost;
            public int SituationType;
            public int ActionType;
            public int Flags;
            public int Duration;
            public int RangeShape;
            public int RangeValue;
            public int IsAdjustableRange;
            public int TargetingMode;
            public int TargetingRange;
            public int IsMovingSkill;
            public int IsCooperative;
            public float AddAtkMag;
            public float AddDefMag;
            public int AddAtkNum;
            public float Param1;
            public float Param2;
            public float Param3;
            public float Param4;
            public string ExplainTextKey;
        }

        [System.Serializable]
        private class SkillDataContainer
        {
            public FileData[] SkillsData;
        }
#pragma warning restore 0649

        private const string ResourcesPath = "SkillData/SkillData";

        static public Data[] data = new Data[( int ) SkillID.NUM];
        // MEMO : 攻撃を受けた際に反応するパリィのような「リアクション型」のスキルのみを登録します。
        //        大半のスキルはリアクションを必要としないため、ここには登録しません(登録が無ければ通知処理自体が存在しません)。
        static public Dictionary<SkillID, Func<SkillNotifierBase>> ReactiveSkillNotifierFactory = null;
        static private readonly SkillActionBase sharedAction            = new SkillActionBase(null);    // 使いまわし前提のため静的読み取り専用

        // MEMO : データだけでは表現できない固有の手続き(移動・アニメーション制御等)を持つスキルのみを登録します。
        //        登録が無いスキルはsharedAction(汎用実装、Dataの数値を元にダメージ計算等を行う)を使用します。
        static private Dictionary<SkillID, Func<Character, List<CharacterKey>, HierarchyBuilderBase, SkillActionBase>> _skillActionFactory =
            new Dictionary<SkillID, Func<Character, List<CharacterKey>, HierarchyBuilderBase, SkillActionBase>>
            {
                { SkillID.DOUBLE_STRIKE, ( owner, targets, hierarchyBld ) => hierarchyBld.InstantiateWithDiContainer<PartOfRangeSABase>( new object[] { owner, targets }, false ) },
                { SkillID.DASH_SLASH,    ( owner, targets, hierarchyBld ) => hierarchyBld.InstantiateWithDiContainer<DashSlashSA>( new object[] { owner, targets }, false ) },
                { SkillID.JUMP_SLASH,    ( owner, targets, hierarchyBld ) => hierarchyBld.InstantiateWithDiContainer<JumpSlashSA>( new object[] { owner, targets }, false ) },
            };

        static SkillsData()
        {
            Load();
        }

        /// <summary>
        /// Resources/SkillData/SkillData.json からスキルデータを読み込みます。
        /// どのシーンから起動しても最初のアクセス時に一度だけ読み込まれ、以後はシーンを跨いで保持されます。
        /// </summary>
        static private void Load()
        {
            var asset = Resources.Load<TextAsset>( ResourcesPath );
            if ( asset == null )
            {
                Debug.LogWarning( $"[SkillsData] スキルデータが見つかりません: Resources/{ResourcesPath}.json" );
                return;
            }

            var container = JsonUtility.FromJson<SkillDataContainer>( asset.text );
            if ( container == null || container.SkillsData == null )
            {
                Debug.LogWarning( "[SkillsData] スキルデータの読み込みに失敗しました" );
                return;
            }

            int count = Mathf.Min( container.SkillsData.Length, data.Length );
            for ( int i = 0; i < count; ++i )
            {
                ApplyFileData( ref data[i], container.SkillsData[i] );
            }
        }

        static private void ApplyFileData( ref Data data, in FileData fdata )
        {
            data.Name               = fdata.Name;
            data.Cost                = fdata.Cost;
            data.SituationType      = ( SituationType ) fdata.SituationType;
            data.ActionType          = ( ActionType ) fdata.ActionType;
            data.Flags               = ( SkillBitFlag ) fdata.Flags;
            data.Duration            = fdata.Duration;
            data.RangeShape          = ( RangeShape ) fdata.RangeShape;
            data.RangeValue          = fdata.RangeValue;
            data.IsAdjustableRange   = ( 0 < fdata.IsAdjustableRange );
            data.TargetingMode      = ( TargetingMode ) fdata.TargetingMode;
            data.TargetingRange      = fdata.TargetingRange;
            data.IsMovingSkill       = ( 0 < fdata.IsMovingSkill );
            data.IsCooperative       = ( 0 < fdata.IsCooperative );
            data.AddAtkMag           = fdata.AddAtkMag;
            data.AddDefMag           = fdata.AddDefMag;
            data.AddAtkNum           = fdata.AddAtkNum;
            data.Param1              = fdata.Param1;
            data.Param2              = fdata.Param2;
            data.Param3              = fdata.Param3;
            data.Param4              = fdata.Param4;
            data.ExplainTextKey      = ParseExplainTextKey( fdata.ExplainTextKey );
        }

        // MEMO : ExplainTextKeyはJSON上では可読性のためキー名の文字列(例: "EXPL_SKILL_DASH_SLASH")で保持しており、
        //        他のenumフィールドのようなint格納とは意図的に統一していない。
        static private LocKey ParseExplainTextKey( string keyName )
        {
            if ( string.IsNullOrEmpty( keyName ) ) { return LocKey.None; }

            if ( Enum.TryParse<LocKey>( keyName, out var key ) ) { return key; }

            Debug.LogWarning( $"[SkillsData] ExplainTextKeyが不正です: {keyName}" );
            return LocKey.None;
        }

        static public void BuildSkillNotifierFactory( HierarchyBuilderBase hierarchyBld )
        {
            if( ReactiveSkillNotifierFactory != null ) { return; }

            ReactiveSkillNotifierFactory = new Dictionary<SkillID, Func<SkillNotifierBase>>
            {
                { SkillID.PARRY, () => hierarchyBld.InstantiateWithDiContainer<ParrySkillNotifier>(false) },
            };
        }

        static public SkillActionBase CreateSkillAction( SkillID skillID, Character owner, List<CharacterKey> targetCharaKeys, HierarchyBuilderBase hierarchyBld )
        {
            return _skillActionFactory.TryGetValue( skillID, out var factory )
                ? factory( owner, targetCharaKeys, hierarchyBld )
                : sharedAction;
        }

        static public bool IsValidSkill( SkillID skillID )
        {
            return SkillID.NONE < skillID && skillID < SkillID.NUM;
        }

        static public bool IsTransitionSkillActionType( ActionType actType )
        {
            if( actType == ActionType.ATTACK || actType == ActionType.HEAL || actType == ActionType.SUPPORT ) { return true; }

            return false;
        }
    }
}