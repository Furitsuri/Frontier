using Frontier.Entities;
using Frontier.Sequences;

namespace Frontier.Combat
{
    public class SkillActionBase : ISequence
    {
        protected Character _owner = null;

        public SkillActionBase( Character owner )
        {
            _owner = owner;
        }

        public void Start()
        {
            StartAction();
        }

        public void End()
        {
            EndAction();
        }

        public bool Update()
        {
            UpdateAction();

            return IsFinished();
        }

        protected virtual void StartAction()
        {
        }

        protected virtual void EndAction()
        {
        }

        protected virtual void UpdateAction()
        {
        }

        protected virtual bool IsFinished()
        {
            return true;
        }
    }
}