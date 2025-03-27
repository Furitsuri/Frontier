using Frontier.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities
{
    public class Npc : Character
    {
        /// <summary>
        /// 思考タイプ
        /// </summary>
        public enum ThinkingType
        {
            AGGERESSIVE = 0,    // 積極的に移動し、攻撃後の結果の評価値が高い対象を狙う
            WAITING,            // 自身の行動範囲に対象が入ってこない限り動かない。動き始めた後はAGGRESSIVEと同じ動作

            NUM
        }

        // 基盤となるAI
        protected BaseAi _baseAI { get; set; } = null;
        // 思考タイプ
        protected ThinkingType _thikType;

        /// <summary>
        /// キャラクターの思考タイプを設定します
        /// </summary>
        /// <param name="type">設定する思考タイプ</param>
        virtual public void SetThinkType( Npc.ThinkingType type ) { }

        /// <summary>
        /// 使用スキルを選択します
        /// </summary>
        /// <param name="type">攻撃、防御、常駐などのスキルタイプ</param>
        override public void SelectUseSkills( SkillsData.SituationType type )
        {

        }

        /// <summary>
        /// 設定されているAIを取得します
        /// </summary>
        /// <returns>設定されているAI</returns>
        public BaseAi GetAi() { return _baseAI; }
    }
}