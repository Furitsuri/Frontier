using Frontier.Combat.Skill;

namespace Frontier.Entities
{
    public class Npc : Character
    {
        override public void Init()
        {
            base.Init();
        }

        /// <summary>
        /// 
        /// </summary>
        virtual public void ToggleAttackableRangeDisplay()
        {
        }

        /// <summary>
        /// 使用スキルを選択します
        /// </summary>
        /// <param name="type">攻撃、防御、常駐などのスキルタイプ</param>
        override public void SelectUseSkills( SituationType type )
        {

        }
    }
}