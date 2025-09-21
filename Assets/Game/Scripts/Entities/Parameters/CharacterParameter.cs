using Frontier.Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontier.Combat.Skill;
using static Frontier.BattleFileLoader;
using static Frontier.Entities.Character;

namespace Frontier.Entities
{
    /// <summary>
    /// キャラクターのパラメータの構造体です
    /// </summary>
    [Serializable]
    public struct CharacterParameter
    {
        public CHARACTER_TAG characterTag;  // キャラクタータグ
        public int characterIndex;          // キャラクター番号
        public int MaxHP;                   // 最大HP
        public int CurHP;                   // 現在HP
        public int Atk;                     // 攻撃力
        public int Def;                     // 防御力
        public int moveRange;               // 移動レンジ
        public int jumpForce;               // ジャンプレンジ
        public int attackRange;             // 攻撃レンジ
        public int maxActionGauge;          // アクションゲージ最大値
        public int curActionGauge;          // アクションゲージ現在値
        public int recoveryActionGauge;     // アクションゲージ回復値
        public int consumptionActionGauge;  // アクションゲージ消費値
        public int initGridIndex;           // ステージ開始時グリッド座標(インデックス)
        public Constants.Direction initDir; // ステージ開始時向き
        public ID[] equipSkills;            // 装備しているスキル

        /// <summary>
        /// キャラクターパラメータを適応させます
        /// ※C#のstructは基本的に値渡しで動作するため、this.CurHP = fdata.MaxHP;などとしても反映されない
        /// </summary>
        /// <param name="param">適応先のキャラクターパラメータ</param>
        /// <param name="fdata">適応元のキャラクターパラメータ</param>
        static public void ApplyParams(ref CharacterParameter param, in CharacterParamData fdata)
        {
            param.characterTag          = (CHARACTER_TAG)fdata.CharacterTag;
            param.characterIndex        = fdata.CharacterIndex;
            param.CurHP                 = param.MaxHP = fdata.MaxHP;
            param.Atk                   = fdata.Atk;
            param.Def                   = fdata.Def;
            param.moveRange             = fdata.MoveRange;
            param.jumpForce             = fdata.MoveRange;  // TODO : エクセルが使用できず、ジャンプレンジのデータを入れられないため、移動レンジと同じ値を入れておく
            param.attackRange           = fdata.AtkRange;
            param.curActionGauge        = param.maxActionGauge = fdata.ActGaugeMax;
            param.recoveryActionGauge   = fdata.ActRecovery;
            param.initGridIndex         = fdata.InitGridIndex;
            param.initDir               = (Constants.Direction)fdata.InitDir;
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                param.equipSkills[i] = (ID )fdata.Skills[i];
            }
        }

        public void Awake()
        {
            equipSkills = new ID[Constants.EQUIPABLE_SKILL_MAX_NUM];
        }

        public void Init()
        {
            characterTag            = CHARACTER_TAG.NONE;
            characterIndex          = 0;
            MaxHP                   = 0;
            CurHP                   = 0;
            Atk                     = 0;
            Def                     = 0;
            moveRange               = 0;
            jumpForce               = 0;
            attackRange             = 0;
            maxActionGauge          = 0;
            curActionGauge          = 0;
            recoveryActionGauge     = 0;
            consumptionActionGauge  = 0;
            initGridIndex           = 0;
            initDir                 = Constants.Direction.NONE;
        }

        /// <summary>
        /// 外部からパラメータを適用させます
        /// </summary>
        /// <param name="fdata">適応元のキャラクターパラメータ</param>
        public void Apply(in CharacterParamData fdata)
        {
            this.characterTag           = (CHARACTER_TAG)fdata.CharacterTag;
            this.characterIndex         = fdata.CharacterIndex;
            this.CurHP                  = this.MaxHP = fdata.MaxHP;
            this.Atk                    = fdata.Atk;
            this.Def                    = fdata.Def;
            this.moveRange              = fdata.MoveRange;
            this.jumpForce              = fdata.JumpRange;
            this.attackRange            = fdata.AtkRange;
            this.curActionGauge         = this.maxActionGauge = fdata.ActGaugeMax;
            this.recoveryActionGauge    = fdata.ActRecovery;
            this.initGridIndex          = fdata.InitGridIndex;
            this.initDir                = (Constants.Direction)fdata.InitDir;
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                this.equipSkills[i] = (ID)fdata.Skills[i];
            }
        }

        /// <summary>
        /// アクションゲージ消費量をリセットします
        /// </summary>
        public void ResetConsumptionActionGauge()
        {
            consumptionActionGauge = 0;
        }

        /// <summary>
        /// アクションゲージをrecoveryActionGaugeの分だけ回復します
        /// 基本的に自ターン開始時に呼びます
        /// </summary>
        public void RecoveryActionGauge()
        {
            curActionGauge = Mathf.Clamp( curActionGauge + recoveryActionGauge, 0, maxActionGauge );
        }

        /// <summary>
        /// 指定したキャラクタータグに合致するかを取得します
        /// </summary>
        /// <param name="tag">指定するキャラクタータグ</param>
        /// <returns>合致しているか否か</returns>
        public bool IsMatchCharacterTag(CHARACTER_TAG tag)
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
        /// 指定のスキルが有効か否かを返します
        /// </summary>
        /// <param name="index">指定インデックス</param>
        /// <returns>有効か否か</returns>
        public bool IsValidSkill(int index)
        {
            return ID.SKILL_NONE < equipSkills[index] && equipSkills[index] < ID.SKILL_NUM;
        }

        /// <summary>
        /// 指定のスキルが使用可能かを判定します
        /// </summary>
        /// <param name="skillIdx">スキルの装備インデックス値</param>
        /// <returns>指定スキルの使用可否</returns>
        public bool CanUseEquipSkill( int skillIdx, SituationType situationType )
        {
            if ( Constants.EQUIPABLE_SKILL_MAX_NUM <= skillIdx )
            {
                Debug.Assert( false, "指定されているスキルの装備インデックス値がスキルの装備最大数を超えています。" );

                return false;
            }

            int skillID = (int)equipSkills[skillIdx];
            var skillData = SkillsData.data[skillID];

            // 同一のシチュエーションでない場合は使用不可(攻撃シチュエーション時に防御スキルは使用出来ない等)
            if ( skillData.Type != situationType )
            {
                return false;
            }

            // コストが現在のアクションゲージ値を越えていないかをチェック
            if ( consumptionActionGauge + skillData.Cost <= curActionGauge )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 装備中のスキルの名前を取得します
        /// </summary>
        /// <returns>スキル名の配列</returns>
        public string[] GetEquipSkillNames()
        {
            string[] names = new string[Constants.EQUIPABLE_SKILL_MAX_NUM];

            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                names[i] = "";
                if ( !IsValidSkill(i) ) continue;
                names[i] = SkillsData.data[(int)equipSkills[i]].Name;
                names[i] = names[i].Replace("_", Environment.NewLine);
            }

            return names;
        }
    }
}
