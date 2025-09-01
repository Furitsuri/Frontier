using Frontier.Combat;
using Frontier.Entities;
using Zenject;

namespace Frontier.Combat.Skill
{
    public class SkillNotifierBase
    {
        protected Character _skillUser = null;    // スキル使用者
        protected CombatSkillEventController _combatSkillEventCtrl = null;

        [Inject]
        void Construct( CombatSkillEventController combatSkillEventCtrl )
        {
            _combatSkillEventCtrl = combatSkillEventCtrl;
        }

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