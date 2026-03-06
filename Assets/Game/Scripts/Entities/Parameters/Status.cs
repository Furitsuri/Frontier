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
        public string Name;                 // キャラクター名
        public int Level;                   // レベル
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
        public int ActGaugeConsumption;     // アクションゲージ消費値
        public int initGridIndex;           // ステージ開始時グリッド座標(インデックス)
        public Direction initDir;           // ステージ開始時向き
        public SkillID[] EquipSkills;       // 装備しているスキル

        /// <summary>
        /// キャラクターパラメータを適応させます
        /// ※C#のstructは基本的に値渡しで動作するため、this.CurHP = fdata.MaxHP;などとしても反映されない
        /// </summary>
        /// <param name="status">適応先のキャラクターパラメータ</param>
        /// <param name="fdata">適応元のキャラクターパラメータ</param>
        static public void ApplyParams( ref Status status, in BattleFileLoader.CharacterStatusData fdata )
        {
            status.characterTag         = ( CHARACTER_TAG ) fdata.CharacterTag;
            status.characterIndex       = fdata.CharacterIndex;
            status.CurHP                = status.MaxHP = fdata.MaxHP;
            status.Atk                  = fdata.Atk;
            status.Def                  = fdata.Def;
            status.moveRange            = fdata.MoveRange;
            status.jumpForce            = fdata.JumpForce;
            status.attackRange          = fdata.AtkRange;
            status.CurActionGauge       = status.maxActionGauge = fdata.ActGaugeMax;
            status.recoveryActionGauge  = fdata.ActRecovery;
            status.initGridIndex        = fdata.InitGridIndex;
            status.initDir              = ( Direction ) fdata.InitDir;

            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                status.EquipSkills[i] = ( SkillID ) fdata.Skills[i];
            }
        }

        public void Setup()
        {
            EquipSkills = new SkillID[Constants.EQUIPABLE_SKILL_MAX_NUM];
        }

        public void Init()
        {
            characterTag        = CHARACTER_TAG.NONE;
            characterIndex      = 0;
            Name                = "";
            Level               = 1;
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
            ActGaugeConsumption = 0;
            initGridIndex       = 0;
            initDir             = Direction.NONE;
        }

        /// <summary>
        /// 外部からパラメータを適用させます
        /// </summary>
        /// <param name="fdata">適応元のキャラクターパラメータ</param>
        public void Apply( in BattleFileLoader.CharacterStatusData fdata )
        {
            this.characterTag           = ( CHARACTER_TAG ) fdata.CharacterTag;
            this.characterIndex         = fdata.CharacterIndex;
            this.Name                   = fdata.Name;
            this.Level                  = fdata.Level;
            this.CurHP                  = this.MaxHP = fdata.MaxHP;
            this.Atk                    = fdata.Atk;
            this.Def                    = fdata.Def;
            this.moveRange              = fdata.MoveRange;
            this.jumpForce              = fdata.JumpForce;
            this.attackRange            = fdata.AtkRange;
            this.CurActionGauge         = this.maxActionGauge = fdata.ActGaugeMax;
            this.recoveryActionGauge    = fdata.ActRecovery;
            this.initGridIndex          = fdata.InitGridIndex;
            this.initDir                = ( Direction ) fdata.InitDir;

            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                this.EquipSkills[i] = ( SkillID ) fdata.Skills[i];
            }
        }

        /// <summary>
        /// アクションゲージ消費量をリセットします
        /// </summary>
        public void ResetConsumptionActionGauge()
        {
            ActGaugeConsumption = 0;
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
        /// 装備中のスキルの名前を取得します
        /// </summary>
        /// <returns>スキル名の配列</returns>
        public string[] GetEquipSkillNames()
        {
            string[] names = new string[Constants.EQUIPABLE_SKILL_MAX_NUM];

            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
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