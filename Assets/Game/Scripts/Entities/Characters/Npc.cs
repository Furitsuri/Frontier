using Frontier.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontier.Combat.Skill;

namespace Frontier.Entities
{
    public class Npc : Character
    {
        // 基盤となるAI
        protected BaseAi _baseAI { get; set; } = null;
        // 思考タイプ
        protected ThinkingType _thikType;

        /// <summary>
        /// キャラクターの思考タイプを設定します
        /// </summary>
        /// <param name="type">設定する思考タイプ</param>
        virtual public void SetThinkType( ThinkingType type ) { }

        /// <summary>
        /// 使用スキルを選択します
        /// </summary>
        /// <param name="type">攻撃、防御、常駐などのスキルタイプ</param>
        override public void SelectUseSkills( SituationType type )
        {

        }

        /// <summary>
        /// 設定されているAIを取得します
        /// </summary>
        /// <returns>設定されているAI</returns>
        public BaseAi GetAi() { return _baseAI; }
    }
}