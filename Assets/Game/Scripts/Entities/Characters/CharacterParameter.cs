using Frontier.Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        public int attackRange;             // 攻撃レンジ
        public int maxActionGauge;          // アクションゲージ最大値
        public int curActionGauge;          // アクションゲージ現在値
        public int recoveryActionGauge;     // アクションゲージ回復値
        public int consumptionActionGauge;  // アクションゲージ消費値
        public int initGridIndex;           // ステージ開始時グリッド座標(インデックス)
        public Constants.Direction initDir; // ステージ開始時向き
        public SkillsData.ID[] equipSkills; // 装備しているスキル

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
            return SkillsData.ID.SKILL_NONE < equipSkills[index] && equipSkills[index] < SkillsData.ID.SKILL_NUM;
        }

        /// <summary>
        /// 指定のスキルが使用可能かを判定します
        /// </summary>
        /// <param name="skillIdx">スキルの装備インデックス値</param>
        /// <returns>指定スキルの使用可否</returns>
        public bool CanUseEquipSkill(int skillIdx, SkillsData.SituationType situationType)
        {
            if (Constants.EQUIPABLE_SKILL_MAX_NUM <= skillIdx)
            {
                Debug.Assert(false, "指定されているスキルの装備インデックス値がスキルの装備最大数を超えています。");

                return false;
            }

            int skillID = (int)equipSkills[skillIdx];
            var skillData = SkillsData.data[skillID];
            
            // 同一のシチュエーションでない場合は使用不可(攻撃シチュエーション時に防御スキルは使用出来ない等)
            if( skillData.Type != situationType )
            {
                return false;
            }

            // コストが現在のアクションゲージ値を越えていないかをチェック
            if (consumptionActionGauge + skillData.Cost <= curActionGauge)
            {
                return true;
            }

            return false;
        }
    }
}
