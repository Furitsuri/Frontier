using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;
using static Constants;
using static InputCode;

namespace Frontier.Battle
{
    public class PlSkillActionToTargetState : PlPhaseStateBase
    {
        private enum TransitTag
        {
            CHARACTER_STATUS = 0,
            USE_SKILL_OPTION  = 1,
        }

        protected enum PlSkillActionPhase : int
        {
            PL_SKILL_ACTION_SELECT_GRID = 0,
            PL_SKILL_ACTION_EXECUTE,
            PL_SKILL_ACTION_END,
        }

        private bool _isWaitingForOptionResult;
        private bool _isSkillQueued;
        private SkillID _useSkillID;
        private PlSkillActionPhase _phase;

        private SequenceFacade _sequenceFcd                   = null;
        private SkillActionReservationQueue _reservationQueue = null;
        private SkillTargetSelector _targetSelector           = null;
        private CooperativeBlinkController _blinkController   = null;

        [Inject] public PlSkillActionToTargetState( BattleRoutineController btlRtnCtrl, SequenceFacade sequenceFcd, SkillActionReservationQueue reservationQueue )
        {
            _btlRtnCtrl       = btlRtnCtrl;
            _sequenceFcd      = sequenceFcd;
            _reservationQueue = reservationQueue;

            _targetSelector   = new SkillTargetSelector();
            _blinkController  = new CooperativeBlinkController( reservationQueue, btlRtnCtrl );
        }

        /// <summary>
        /// 初期化します
        /// MEMO : 攻撃範囲については前StateのPlSelectSkillStateで設定済みであるため、ここでは設定しないことに注意
        /// </summary>
        public override void Init( object context )
        {
            base.Init( context );

            _isWaitingForOptionResult = false;
            _isSkillQueued            = false;

            ReceiveContext( ref _useSkillID, context );
            _stageCtrl.BindGridCursor( GridCursorState.ATTACK, _plOwner );

            _targetSelector.Init( SkillsData.data[( int ) _useSkillID], BuildTargetingContext() );

            _phase = PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID;
            _blinkController.Refresh( _targetSelector.AttackTargetCharaKeys, _useSkillID );
        }

        public override bool Update()
        {
            bool isActiveRightParameterView = ( _targetSelector.TargetCharacter != null );
            _presenter.CharaParamView( ParameterWindowType.Right ).SetActive( isActiveRightParameterView );
            if( isActiveRightParameterView )
            {
                var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Right );
                _presenter.CharaParamView( ParameterWindowType.Right ).AssignCharacter( _targetSelector.TargetCharacter, layerMaskIndex );
            }

            if( base.Update() ) { return true; }

            switch( _phase )
            {
                case PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID:
                    break;
                case PlSkillActionPhase.PL_SKILL_ACTION_EXECUTE:
                    _phase = PlSkillActionPhase.PL_SKILL_ACTION_END;
                    break;
                case PlSkillActionPhase.PL_SKILL_ACTION_END:
                    _plOwner.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.SKILL, true );
                    _plOwner.ClearCommandHistory();
                    Back();
                    return true;
                default:
                    break;
            }

            return false;
        }

        public override object ExitState()
        {
            _blinkController.StopAll();
            OnExitStateAfterCombat( _plOwner, _targetSelector.TargetCharacter );

            if( _isSkillQueued )
            {
                _plOwner.BattleLogic.ActionRangeCtrl.DrawTargetableRangeAsQueued();
            }
            else
            {
                _plOwner.CleanupGhost();
            }

            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR,  "SELECT\nTILE", CanAcceptDirection, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM,     "CONFIRM", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
                (GuideIcon.CANCEL,      "BACK", CanAcceptCancel, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode),
                (GuideIcon.INFO,        "STATUS", CanAcceptInfo, new AcceptContextInput( AcceptInfo ), 0.0f, hashCode),
                (new GuideIcon[] { GuideIcon.SUB1, GuideIcon.SUB2 }, "CHANGE\nCHARACTER", new EnableCallback[] { CanAcceptSub1, CanAcceptSub2 }, new IAcceptInputBase[] { new AcceptContextInput( AcceptSub1 ), new AcceptContextInput( AcceptSub2 ) }, 0.0f, hashCode),
                (new GuideIcon[] { GuideIcon.SUB3, GuideIcon.SUB4 }, "ADJUST\nPOWER",    new EnableCallback[] { CanAcceptSub3, CanAcceptSub4 }, new IAcceptInputBase[] { new AcceptContextInput( AcceptSub3 ), new AcceptContextInput( AcceptSub4 ) }, 0.0f, hashCode)
            );
        }

        protected override void AdaptSelectPlayer()
        {
            var selectCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            _plOwner            = _btlRtnCtrl.BtlCharaCdr.GetPlayer( selectCharacter.GetCharacterKey() );
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Left );
            _presenter.CharaParamView( ParameterWindowType.Left ).AssignCharacter( _plOwner, layerMaskIndex );

            if( _isWaitingForOptionResult )
            {
                _isWaitingForOptionResult = false;
                var optionState = GetChildren<PlSkillUseOptionState>( ( int ) TransitTag.USE_SKILL_OPTION );

                switch( optionState?.SelectedOption )
                {
                    case USE_SKILL_OPTION_TAG.EXECUTION:
                        ExecuteSkill();
                        break;

                    case USE_SKILL_OPTION_TAG.QUEUE:
                        _isSkillQueued = true;
                        EnqueueSkillAction();
                        CleanupEnqueuedAction();
                        Back();
                        break;

                    case USE_SKILL_OPTION_TAG.COOPERATIVE:
                        ExecuteCooperativeSkill();
                        break;

                    // PlSkillUseOptionStateの選択をキャンセルされた場合
                    default:
                        _plOwner.BattleLogic.ActionRangeCtrl.ReDrawAttackableRange();
                        _blinkController.Refresh( _targetSelector.AttackTargetCharaKeys, _useSkillID );
                        break;
                }
            }
        }

        protected override bool CanAcceptDefault()
        {
            if( _phase != PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID ) { return false; }
            return base.CanAcceptDefault();
        }

        protected override bool CanAcceptDirection() => CanAcceptDefault();

        protected override bool CanAcceptConfirm()
        {
            if( _phase != PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID ) { return false; }
            return _targetSelector.HasTarget;
        }

        protected override bool CanAcceptCancel() => _phase == PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID;

        protected override bool CanAcceptInfo()
        {
            if( !CanAcceptDefault() ) { return false; }

            var selectCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( selectCharacter == null || selectCharacter == _plOwner ) { return false; }

            return true;
        }

        protected override bool CanAcceptSub1() { return _phase == PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID && 1 < _targetSelector.AttackTargetCharaKeys.Count; }
        protected override bool CanAcceptSub2() { return CanAcceptSub1(); }

        protected override bool CanAcceptSub3() => _phase == PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID && _targetSelector.IsAdjustableRange && 1 < _targetSelector.CurrentRange;
        protected override bool CanAcceptSub4() => _phase == PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID && _targetSelector.IsAdjustableRange && _targetSelector.CurrentRange < _targetSelector.MaxRange;

        protected override bool AcceptDirection( InputContext context )
        {
            context.Cursor = _stageCtrl.ConvertDirectionDependOnCameraAngle( context.Cursor );
            if( Direction.NONE == context.Cursor ) { return false; }

            _targetSelector.AcceptDirection( context.Cursor, BuildTargetingContext() );
            _blinkController.Refresh( _targetSelector.AttackTargetCharaKeys, _useSkillID );

            return true;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            if( SkillsData.data[( int ) _useSkillID].IsCooperative )
            {
                var cooperativeAttackers = _blinkController.GetCooperativeAttackers( _targetSelector.AttackTargetCharaKeys );
                var options = ( cooperativeAttackers.Count > 0 )
                    ? new List<USE_SKILL_OPTION_TAG> { USE_SKILL_OPTION_TAG.COOPERATIVE, USE_SKILL_OPTION_TAG.QUEUE }
                    : new List<USE_SKILL_OPTION_TAG> { USE_SKILL_OPTION_TAG.EXECUTION,   USE_SKILL_OPTION_TAG.QUEUE };

                // 選択した攻撃範囲以外のタイル描画を全てクリアする
                _plOwner.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.MOVEABLE | TileMapType.ATTACKABLE );

                SetSendTransitionContext( new SkillUseOptionContext( options, cooperativeAttackers ) );
                _isWaitingForOptionResult = true;
                TransitState( ( int ) TransitTag.USE_SKILL_OPTION );
            }
            else
            {
                ExecuteSkill();
            }

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            _targetSelector.TargetCharacter?.GetTransformHandler.ResetRotationOrder();

            return true;
        }

        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            SetSendTransitionContext( _btlRtnCtrl.BtlCharaCdr.GetTargetCharacter() );
            TransitState( ( int ) TransitTag.CHARACTER_STATUS );

            return true;
        }

        protected override bool AcceptSub1( InputContext context )
        {
            if( !base.AcceptSub1( context ) ) { return false; }
            SwitchFocusedTarget( Direction.LEFT );
            return true;
        }

        protected override bool AcceptSub2( InputContext context )
        {
            if( !base.AcceptSub2( context ) ) { return false; }
            SwitchFocusedTarget( Direction.RIGHT );
            return true;
        }

        protected override bool AcceptSub3( InputContext context )
        {
            if( !base.AcceptSub3( context ) ) { return false; }

            bool adjusted = _targetSelector.TryAdjustRange( -1, BuildTargetingContext() );
            if( adjusted ) { _blinkController.Refresh( _targetSelector.AttackTargetCharaKeys, _useSkillID ); }
            return adjusted;
        }

        protected override bool AcceptSub4( InputContext context )
        {
            if( !base.AcceptSub4( context ) ) { return false; }

            bool adjusted = _targetSelector.TryAdjustRange( +1, BuildTargetingContext() );
            if( adjusted ) { _blinkController.Refresh( _targetSelector.AttackTargetCharaKeys, _useSkillID ); }
            return adjusted;
        }

        private void SwitchFocusedTarget( Direction dir )
        {
            if( _stageCtrl.OperateTargetSelect( dir ) )
            {
                _targetSelector.UpdateFocusedTarget( _btlRtnCtrl.BtlCharaCdr.GetTargetCharacter() );
                _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( _plOwner, _targetSelector.TargetCharacter );
            }
        }

        private void ExecuteSkill()
        {
            _plOwner.BattleLogic.ConsumeActionGaugeForSkill();
            _plOwner.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.ATTACKABLE | TileMapType.TARGETABLE | TileMapType.QUEUED );
            _targetSelector.TargetCharacter?.BattleLogic.ConsumeActionGauge();

            _stageCtrl.SetActiveGridCursor( false );
            _stageCtrl.SetActiveTargetCursor( false );
            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );

            if( _plOwner.BattleLogic.RegistSelfBuffSequences() )
            {
                _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
            }

            _sequenceFcd.RegistSkillAction( _plOwner, _targetSelector.TargetCharacter, _useSkillID, _targetSelector.AttackTargetCharaKeys );

            _phase = PlSkillActionPhase.PL_SKILL_ACTION_EXECUTE;
        }

        private void EnqueueSkillAction()
        {
            _plOwner.BattleParams.TmpParam.AssignExpectedHpChange( out int attackerHpChange, out int attackerTotalHpChange );

            int targetHpChange = 0, targetTotalHpChange = 0;
            var focusedTargetKey = CharacterKey.Invalid;
            if( _targetSelector.TargetCharacter != null )
            {
                _targetSelector.TargetCharacter.BattleParams.TmpParam.AssignExpectedHpChange( out targetHpChange, out targetTotalHpChange );
                focusedTargetKey = _targetSelector.TargetCharacter.GetCharacterKey();
            }

            var data = new SkillActionReservationData(
                _plOwner.GetCharacterKey(),
                _plOwner.BattleParams.TmpParam.CurrentTileIndex,
                _plOwner.BattleParams.TmpParam.IsSkillsToggledON,
                _plOwner.BattleParams.TmpParam.ActGaugeConsumption,
                _useSkillID,
                _targetSelector.TargetingMode,
                _targetSelector.CurrentRange,
                _targetSelector.MaxRange,
                _targetSelector.IsAdjustableRange,
                _targetSelector.IsMovingSkill,
                _targetSelector.AttackTargetCharaKeys,
                focusedTargetKey,
                attackerHpChange,
                attackerTotalHpChange,
                targetHpChange,
                targetTotalHpChange,
                _plOwner.GhostObj?.TileIndex ?? -1
            );

            _reservationQueue.Enqueue( data );
        }

        private void ExecuteCooperativeSkill()
        {
            var cooperativeAttackers = _blinkController.GetCooperativeAttackers( _targetSelector.AttackTargetCharaKeys );
            var entries = new List<CooperativeSkillEntry>( cooperativeAttackers.Count + 1 );

            foreach( var attacker in cooperativeAttackers )
            {
                if( !_reservationQueue.TryDequeueByAttackerKey( attacker.GetCharacterKey(), out var data ) ) { continue; }

                if( data.GhostTileIndex >= 0 )
                {
                    var ghost = attacker.GetGhostObject();
                    ghost.TileIndex          = data.GhostTileIndex;
                    ghost.transform.position = _stageCtrl.GetTileStaticData( data.GhostTileIndex ).CharaStandPos;
                }

                for( int i = 0; i < data.AttackerSkillsToggledON.Length; ++i )
                {
                    attacker.BattleParams.TmpParam.IsSkillsToggledON[i] = data.AttackerSkillsToggledON[i];
                }
                attacker.BattleParams.TmpParam.ActGaugeConsumption = data.ActGaugeConsumption;

                attacker.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.QUEUED );
                attacker.BattleLogic.ConsumeActionGaugeForSkill();

                var target = data.FocusedTargetCharaKey.IsValid()
                    ? _btlRtnCtrl.BtlCharaCdr.GetCharacter( data.FocusedTargetCharaKey )
                    : null;
                if( target != null ) { target.BattleLogic.ConsumeActionGauge(); }

                if( attacker.BattleLogic.RegistSelfBuffSequences() )
                {
                    attacker.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
                }

                entries.Add( _sequenceFcd.CreateCooperativeEntry( data.UseSkillID, attacker, new List<CharacterKey>( data.AttackTargetCharaKeys ) ) );
            }

            _plOwner.BattleLogic.ConsumeActionGaugeForSkill();
            _plOwner.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.ATTACKABLE | TileMapType.TARGETABLE | TileMapType.QUEUED );
            _targetSelector.TargetCharacter?.BattleLogic.ConsumeActionGauge();

            _stageCtrl.SetActiveGridCursor( false );
            _stageCtrl.SetActiveTargetCursor( false );
            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );

            if( _plOwner.BattleLogic.RegistSelfBuffSequences() )
            {
                _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
            }

            entries.Add( _sequenceFcd.CreateCooperativeEntry( _useSkillID, _plOwner, _targetSelector.AttackTargetCharaKeys ) );

            _sequenceFcd.RegistCooperativeSkillAction( entries );

            _phase = PlSkillActionPhase.PL_SKILL_ACTION_EXECUTE;
        }

        private void CleanupEnqueuedAction()
        {
            _plOwner.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.SKILL, true );
            _plOwner.ClearCommandHistory();

            _stageCtrl.SetActiveGridCursor( false );
            _stageCtrl.SetActiveTargetCursor( false );
            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();
        }

        private TargetingRangeContext BuildTargetingContext()
        {
            return new TargetingRangeContext { BtlRtnCtrl = _btlRtnCtrl, Presenter = _presenter, Owner = _plOwner, StageCtrl = _stageCtrl };
        }
    }
}
