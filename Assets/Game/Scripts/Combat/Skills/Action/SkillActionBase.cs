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

        /// <summary>
        /// スキル名表示の直前に呼ばれます。デフォルトではゴーストを非表示にします。
        /// ゴーストの参照は保持されるため、サブクラスは StartAction() 内で引き続き参照できます。
        /// </summary>
        public virtual void OnBeforeNameDisplay()
        {
            _owner?.GhostObj?.gameObject.SetActive( false );
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