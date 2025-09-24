using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities.Ai;
using System;
using UnityEngine;
using static Constants;

namespace Frontier.Entities
{
    public class Npc : Character
    {
        /// <summary>
        /// 使用スキルを選択します
        /// </summary>
        /// <param name="type">攻撃、防御、常駐などのスキルタイプ</param>
        override public void SelectUseSkills( SituationType type )
        {

        }
    }
}