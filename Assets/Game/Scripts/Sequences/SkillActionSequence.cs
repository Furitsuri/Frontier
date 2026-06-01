using Frontier.Combat;
using Frontier.UI;
using Zenject;

namespace Frontier.Sequences
{
    public class SkillActionSequence : ISequence
    {
        private enum Phase { SHOW_SKILL_NAME, EXECUTING }

        private readonly SkillActionBase _skillAction;
        private readonly SkillID _skillId;
        private readonly IUiSystem _uiSystem;

        private Phase _phase;
        private bool _skillStarted;

        [Inject]
        public SkillActionSequence( SkillActionBase skillAction, SkillID skillId, IUiSystem uiSystem )
        {
            _skillAction = skillAction;
            _skillId     = skillId;
            _uiSystem    = uiSystem;
        }

        public void Start()
        {
            _phase        = Phase.SHOW_SKILL_NAME;
            _skillStarted = false;
            _skillAction.OnBeforeNameDisplay();
            _uiSystem.BattleUi.CommandNameView.Show( _skillId, 0.85f, () => _phase = Phase.EXECUTING );
        }

        public bool Update()
        {
            if( _phase == Phase.SHOW_SKILL_NAME ) { return false; }

            if( !_skillStarted )
            {
                _skillStarted = true;
                _skillAction.Start();
            }

            return _skillAction.Update();
        }

        public void End()
        {
            _skillAction.End();
        }
    }
}
