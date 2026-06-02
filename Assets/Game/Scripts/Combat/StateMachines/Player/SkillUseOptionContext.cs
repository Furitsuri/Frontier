using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;

namespace Frontier.Battle
{
    /// <summary>
    /// PlSkillUseOptionState に渡すコンテキストです。
    /// 表示する選択肢と、連携候補の予約済み攻撃者リストを保持します。
    /// </summary>
    public class SkillUseOptionContext
    {
        /// <summary>表示する選択肢</summary>
        public List<USE_SKILL_OPTION_TAG> Options { get; }

        /// <summary>
        /// 現在のスキルと攻撃対象が重複する予約済みアクションの攻撃者リスト。
        /// 連携候補がない場合は空リスト。
        /// </summary>
        public List<Character> CooperativeAttackers { get; }

        public SkillUseOptionContext( List<USE_SKILL_OPTION_TAG> options, List<Character> cooperativeAttackers )
        {
            Options               = options;
            CooperativeAttackers  = cooperativeAttackers ?? new List<Character>();
        }
    }
}
