using Frontier.Battle;
using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.UI;
using System;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    public class PlSelectSkillState : PlPhaseStateBase
    {
        private enum PlSelectSkillPhase
        {
            PL_SELECT_SKILL = 0,
            PL_SELECT_SKILL_END,
        }

        private enum TransitTag
        {
            SKILL_ACTION_TO_TARGET = 0,
        }

        [Inject] private SequenceFacade _sequenceFcd = null;

        private string[] _playerSkillNames = null;
        private PlSelectSkillPhase _phase = PlSelectSkillPhase.PL_SELECT_SKILL;
        private Func<InputContext, bool>[] AcceptSubs;

        public override void Init( object context )
        {
            base.Init( context);

            _playerSkillNames   = _plOwner.GetStatusRef.GetEquipSkillNames();
            _phase              = PlSelectSkillPhase.PL_SELECT_SKILL;

            AcceptSubs = new Func<InputContext, bool>[]
            {
                ( context ) => base.AcceptSub1( context ),
                ( context ) => base.AcceptSub2( context ),
                ( context ) => base.AcceptSub3( context ),
                ( context ) => base.AcceptSub4( context )
            };

            // 使用可能スキルの更新
            _plOwner.RefreshUseableSkillFlags( Combat.SituationType.ATTACK, 0xff );
        }

        public override bool Update()
        {
            if( base.Update() )
            {
                return true;
            }

            switch( _phase )
            {
                case PlSelectSkillPhase.PL_SELECT_SKILL:

                    break;
                case PlSelectSkillPhase.PL_SELECT_SKILL_END:
                    if( _sequenceFcd.IsEmptySequence() )
                    {
                        Back();

                        return true;
                    }
                    break;
            }

            return false;
        }

        public override object ExitState()
        {
            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.CONFIRM, "CONFIRM", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL, "BACK", CanAcceptCancel, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode),
               (GuideIcon.SUB1, _playerSkillNames[0], CanAcceptSub1, new AcceptContextInput( AcceptSub1 ), 0.0f, hashCode),
               (GuideIcon.SUB2, _playerSkillNames[1], CanAcceptSub2, new AcceptContextInput( AcceptSub2 ), 0.0f, hashCode),
               (GuideIcon.SUB3, _playerSkillNames[2], CanAcceptSub3, new AcceptContextInput( AcceptSub3 ), 0.0f, hashCode),
               (GuideIcon.SUB4, _playerSkillNames[3], CanAcceptSub4, new AcceptContextInput( AcceptSub4 ), 0.0f, hashCode)
            );
        }

        protected override void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            _plOwner = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            // パラメータビューにキャラクターを割り当て
            var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Left );
            _presenter.CharaParamView( ParameterWindowType.Left ).AssignCharacter( _plOwner, layerMaskIndex );
        }

        protected override bool CanAcceptConfirm()
        {
            if( _phase != PlSelectSkillPhase.PL_SELECT_SKILL ) { return false; }

            // 所有スキルのうち、どれか一つでも使用フラグが立っていれば決定入力を受け付ける
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( _plOwner.BattleLogic.IsEquipSkillToggledOn( i ) )
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool CanAcceptCancel()
        {
            if( _phase != PlSelectSkillPhase.PL_SELECT_SKILL ) { return false; }

            return true;
        }

        protected override bool CanAcceptSub1() => CanAcceptSub( 0 );
        protected override bool CanAcceptSub2() => CanAcceptSub( 1 );
        protected override bool CanAcceptSub3() => CanAcceptSub( 2 );
        protected override bool CanAcceptSub4() => CanAcceptSub( 3 );

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            int transitActionIndex = -1;

            // スキル使用フラグが立っているスキルのうち、どれか一つでもターゲット選択に遷移するスキルタイプのものがあれば遷移する
            if( _plOwner.BattleLogic.IsSkillToggledTransitActionState( out transitActionIndex ) )
            {
                SetSendTransitionContext( _plOwner.GetEquipSkillID( transitActionIndex ) );

                TransitState( ( int ) TransitTag.SKILL_ACTION_TO_TARGET );
            }
            else
            {
                // スキル使用フラグが立っているスキルの消費分だけ行動ゲージを減らす
                // (一時保存パラメータに保存することで、キャンセルによって消費前に戻せる形に)
                _plOwner.BattleLogic.ConsumeActionGaugeForSkill();
                // スキルを使用した場合は以前の状態には戻せないため、行動履歴をクリア
                _plOwner.ClearCommandHistory();

                _phase = PlSelectSkillPhase.PL_SELECT_SKILL_END;
            }

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            _plOwner.BattleLogic.RevertSkillsToggledOn();

            return true;
        }

        protected override bool AcceptSub1( InputContext context ) => AcceptSub( 0, context );
        protected override bool AcceptSub2( InputContext context ) => AcceptSub( 1, context );
        protected override bool AcceptSub3( InputContext context ) => AcceptSub( 2, context );
        protected override bool AcceptSub4( InputContext context ) => AcceptSub( 3, context );

        private bool CanAcceptSub( int index )
        {
            if( _phase != PlSelectSkillPhase.PL_SELECT_SKILL )      { return false; }
            if( _playerSkillNames[index].Length <= 0 )              { return false; }

            return _plOwner.BattleParams.TmpParam.IsUseableSkill[index];
        }

        private bool AcceptSub( int index, InputContext context )
        {
            if( !AcceptSubs[index]( context ) ) { return false; }

            _plOwner.BattleLogic.ToggleEquipSkill( index );
            _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, 0xff );  // 使用可能スキルの更新

            return true;
        }
    }
}