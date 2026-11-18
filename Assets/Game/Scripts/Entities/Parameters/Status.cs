using System;
using UnityEngine;
using Frontier.Combat;
using Frontier.Loaders;

namespace Frontier.Entities
{
    /// <summary>
    /// キャラクターのパラメータの構造体です
    /// </summary>
    [Serializable]
    public struct Status
    {
        public CHARACTER_TAG characterTag;  // キャラクタータグ
        public int characterIndex;          // キャラクター番号
        public int PrefabIndex;             // 使用するプレハブのインデックス
        public string Name;                 // キャラクター名
        public int Level;                   // レベル
        public int Exp;                     // 現在のレベル内で取得済みの経験値(レベルアップ時に0リセットされる。LevelExpDataと突き合わせて次のレベルまでの必要経験値を算出する)
        public int StatusPoint;             // 未使用の成長ポイント(レベルアップ時に加算され、任意のステータスへ割り振れる。0〜Constants.STATUS_POINT_MAX)
        public int MaxHP;                   // 最大HP
        public int CurHP;                   // 現在HP
        public int Atk;                     // 攻撃力
        public int Def;                     // 防御力
        public int moveRange;               // 移動レンジ
        public int jumpForce;               // ジャンプレンジ
        public int attackRange;             // 攻撃レンジ( 高低差にもそのまま適用される )
        public int maxActionGauge;          // アクションゲージ最大値
        public int CurActionGauge;          // アクションゲージ現在値
        public int recoveryActionGauge;     // アクションゲージ回復値
        public SkillID[] EquipSkills;       // 装備しているアクティブスキル(EQUIPABLE_ACTIVE_SKILL_MAX_NUM枠)
        public SkillID[] PassiveSkills;     // 装備しているパッシブスキル(EQUIPABLE_PASSIVE_SKILL_MAX_NUM枠)

        /// <summary>
        /// キャラクターパラメータを適応させます
        /// ※C#のstructは基本的に値渡しで動作するため、this.CurHP = fdata.MaxHP;などとしても反映されない
        /// </summary>
        /// <param name="status">適応先のキャラクターパラメータ</param>
        /// <param name="fdata">適応元のキャラクターパラメータ</param>
        static public void ApplyParams( ref Status status, in BattleFileLoader.CharacterDeployData fdata )
        {
            status = fdata.status;
            status.EnsureSkillSlots();
            status.CurHP           = status.MaxHP;
            status.CurActionGauge  = status.maxActionGauge;
        }

        /// <summary>
        /// 装備スキル枠を確保し、全枠を未装備(SkillID.NONE)で初期化します。
        /// MEMO : SkillIDの0はPARRYであるため、配列を確保しただけの状態(全要素0)では全枠にPARRYが入ってしまう点に注意してください。
        /// </summary>
        public void Setup()
        {
            EquipSkills     = CreateEmptySkillSlots( Constants.EQUIPABLE_ACTIVE_SKILL_MAX_NUM );
            PassiveSkills   = CreateEmptySkillSlots( Constants.EQUIPABLE_PASSIVE_SKILL_MAX_NUM );
        }

        /// <summary>
        /// 装備スキル枠が未確保、または枠数が定数と一致しない場合に、枠数を揃えます。
        /// 既存の装備内容は保持し、不足する枠はSkillID.NONEで補完します(超過分は切り捨てます)。
        /// パッシブスキル枠追加以前のセーブデータ・敵データJSON等、該当フィールドを持たないデータを読み込んだ際の補完に用います。
        /// </summary>
        public void EnsureSkillSlots()
        {
            EquipSkills     = ResizeSkillSlots( EquipSkills, Constants.EQUIPABLE_ACTIVE_SKILL_MAX_NUM );
            PassiveSkills   = ResizeSkillSlots( PassiveSkills, Constants.EQUIPABLE_PASSIVE_SKILL_MAX_NUM );
        }

        static private SkillID[] CreateEmptySkillSlots( int slotNum )
        {
            var slots = new SkillID[slotNum];
            for( int i = 0; i < slotNum; ++i ) { slots[i] = SkillID.NONE; }

            return slots;
        }

        static private SkillID[] ResizeSkillSlots( SkillID[] source, int slotNum )
        {
            if( source != null && source.Length == slotNum ) { return source; }

            var slots = CreateEmptySkillSlots( slotNum );
            if( source != null )
            {
                Array.Copy( source, slots, Math.Min( source.Length, slotNum ) );
            }

            return slots;
        }

        public void Init()
        {
            characterTag        = CHARACTER_TAG.NONE;
            characterIndex      = 0;
            Name                = "";
            Level               = 1;
            Exp                 = 0;
            StatusPoint         = 0;
            MaxHP               = 0;
            CurHP               = 0;
            Atk                 = 0;
            Def                 = 0;
            moveRange           = 0;
            jumpForce           = 0;
            attackRange         = 0;
            maxActionGauge      = 0;
            CurActionGauge      = 0;
            recoveryActionGauge = 0;

        }

        /// <summary>
        /// アクションゲージをrecoveryActionGaugeの分だけ回復します
        /// 基本的に自ターン開始時に呼びます
        /// </summary>
        public void RecoveryActionGauge()
        {
            CurActionGauge = Mathf.Clamp( CurActionGauge + recoveryActionGauge, 0, maxActionGauge );
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="damage"></param>
        public void AddDamage( int damage )
        {
            CurHP = Mathf.Clamp( CurHP - damage, 0, MaxHP );
        }

        /// <summary>
        /// 未使用の成長ポイントを増減させます(負の値を渡すことでステータスへの割り振り消費にも使えます)。
        /// </summary>
        /// <param name="amount">増減させる量</param>
        public void AddStatusPoint( int amount )
        {
            StatusPoint = Mathf.Clamp( StatusPoint + amount, 0, Constants.STATUS_POINT_MAX );
        }

        /// <summary>
        /// 指定したキャラクタータグに合致するかを取得します
        /// </summary>
        /// <param name="tag">指定するキャラクタータグ</param>
        /// <returns>合致しているか否か</returns>
        public bool IsMatchCharacterTag( CHARACTER_TAG tag )
        {
            return characterTag == tag;
        }

        /// <summary>
        /// 死亡判定を返します
        /// </summary>
        /// <returns>死亡しているか否か</returns>
        public bool IsDead()
        {
            return CurHP <= 0;
        }

        /// <summary>
        /// 装備中のアクティブスキルの名前を取得します
        /// </summary>
        /// <returns>スキル名の配列</returns>
        public string[] GetEquipSkillNames()
        {
            string[] names = new string[Constants.EQUIPABLE_ACTIVE_SKILL_MAX_NUM];

            for( int i = 0; i < Constants.EQUIPABLE_ACTIVE_SKILL_MAX_NUM; ++i )
            {
                names[i] = "";
                if( !SkillsData.IsValidSkill( EquipSkills[i] ) ) { continue; }
                names[i] = SkillsData.data[( int ) EquipSkills[i]].Name;
                names[i] = names[i].Replace( "_", Environment.NewLine );
            }

            return names;
        }
    }
}