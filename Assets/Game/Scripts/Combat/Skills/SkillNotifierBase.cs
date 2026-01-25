using Frontier.Entities;
using Zenject;

namespace Frontier.Combat.Skill
{
    public class SkillNotifierBase
    {
        [Inject] protected CombatSkillEventController _combatSkillEventCtrl = null;

        protected Character _skillUser = null;    // スキル使用者

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="user">スキルの使用者</param>
        virtual public void Init( Character user )
        {
            _skillUser = user;
        }
    }
}