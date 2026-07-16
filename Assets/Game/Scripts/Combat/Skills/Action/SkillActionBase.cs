using Frontier.Entities;
using Frontier.Sequences;
using Frontier.UI;

namespace Frontier.Combat
{
    public class SkillActionBase : ISequence
    {
        protected Character  _owner     = null;
        protected IUiSystem  _uiSystem  = null;

        public SkillActionBase( Character owner )
        {
            _owner = owner;
        }

        public SkillActionBase( Character owner, IUiSystem uiSystem )
        {
            _owner    = owner;
            _uiSystem = uiSystem;
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

        /// <summary>
        /// 対象キャラクターにダメージを適用し、HPに応じて死亡/被弾アニメーションを再生します。
        /// 連携攻撃中でまだ後続のヒットが予定されている場合(RemainingCooperativeHits参照)は、
        /// HPが0以下でも死亡アニメーションを再生させず、連携最後のヒットまで被弾アニメーションを再生します。
        /// </summary>
        protected void ApplyDamageToTarget( Character target )
        {
            int hpChange = target.BattleParams.TmpParam.ExpectedHpChange;
            target.GetStatusRef.CurHP += hpChange;

            target.BattleParams.TmpParam.ConsumeCooperativeHit();
            bool isFinalHit = target.BattleParams.TmpParam.RemainingCooperativeHits <= 0;

            if( hpChange != 0 )
            {
                if( isFinalHit && target.GetStatusRef.CurHP <= 0 )
                {
                    target.GetStatusRef.CurHP = 0;
                    target.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DIE );
                }
                else
                {
                    target.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.GET_HIT );
                }
            }

            _uiSystem.BattleUi.ShowDamageOnCharacter( target, 1f );
        }
    }
}