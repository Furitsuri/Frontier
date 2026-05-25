using Frontier.Combat;
using Zenject;

namespace Frontier.Sequences
{
    public class SkillActionSequence : ISequence
    {
        private readonly SkillActionBase _skillAction;

        [Inject]
        public SkillActionSequence( SkillActionBase skillAction )
        {
            _skillAction = skillAction;
        }

        public void Start()
        {
            _skillAction.Start();
        }

        public void End()
        {
            _skillAction.End();
        }

        public bool Update()
        {
            return _skillAction.Update();
        }
    }
}