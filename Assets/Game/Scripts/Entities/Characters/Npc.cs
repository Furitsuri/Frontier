using Frontier.Combat.Skill;

namespace Frontier.Entities
{
    public class Npc : Character
    {
        protected AttackableRangeHandler _attackableRangeHandler = null;

        public override void Init()
        {
            base.Init();

            if( _attackableRangeHandler == null )
            {
                _attackableRangeHandler = _hierarchyBld.InstantiateWithDiContainer<AttackableRangeHandler>( false );
                NullCheck.AssertNotNull( _attackableRangeHandler, nameof( _attackableRangeHandler ) );
            }
        }

        public void UnsetAttackableRangeDisplay()
        {
            _attackableRangeHandler.UnsetAttackableRangeDisplay();
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