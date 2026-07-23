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
            CONFIRM_KILL_RESERVED_TARGET = 2,
        }

        protected enum PlSkillActionPhase : int
        {
            PL_SKILL_ACTION_SELECT_GRID = 0,
            PL_SKILL_ACTION_EXECUTE,
            PL_SKILL_ACTION_END,
        }

        private bool _isWaitingForOptionResult;
        private bool _isWaitingForKillConfirmResult;
        private bool _isSkillQueued;
        private bool _isCooperativeSkill;
        private SkillID _useSkillID;
        private PlSkillActionPhase _phase;

        private SequenceFacade _sequenceFcd                     = null;
        private SkillTargetSelector _targetSelector             = null;
        private CooperativeBlinkController _blinkController     = null;
        private CooperativeCandidateFinder _candidateFinder     = null;

        [Inject] public PlSkillActionToTargetState( BattleRoutineController btlRtnCtrl, SequenceFacade sequenceFcd, SkillActionReservationQueue reservationQueue )
        {
            _btlRtnCtrl       = btlRtnCtrl;
            _sequenceFcd      = sequenceFcd;
            _reservationQueue = reservationQueue;

            _targetSelector   = new SkillTargetSelector();
            _blinkController  = new CooperativeBlinkController( reservationQueue, btlRtnCtrl );
            _candidateFinder  = new CooperativeCandidateFinder( reservationQueue, btlRtnCtrl );
        }

        /// <summary>
        /// 初期化します
        /// MEMO : 攻撃範囲については前StateのPlSelectSkillStateで設定済みであるため、ここでは設定しないことに注意
        /// </summary>
        public override void Init( object context )
        {
            base.Init( context );

            _isWaitingForOptionResult      = false;
            _isWaitingForKillConfirmResult = false;
            _isSkillQueued            = false;
            _isCooperativeSkill       = false;

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
                    // 連携攻撃の場合、行動終了の確定は CooperativeSkillSequence.End() で参加キャラクター全員分まとめて行う
                    if( !_isCooperativeSkill ) { _plOwner.FinalizeCommand( COMMAND_TAG.SKILL ); }
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
            CleanupTargetSelectionState( _plOwner, _targetSelector.TargetCharacter );

            if( _isSkillQueued )
            {
                _plOwner.BattleLogic.ActionRangeCtrl.DrawTargetableRangeAsQueued( _plOwner.GhostObj?.TileIndex ?? -1 );
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
                        TryExecuteSkillWithKillConfirm();
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
            else if( _isWaitingForKillConfirmResult )
            {
                _isWaitingForKillConfirmResult = false;
                var confirmState = GetChildren<PlConfirmKillReservedTargetState>( ( int ) TransitTag.CONFIRM_KILL_RESERVED_TARGET );
                if( confirmState != null && confirmState.Confirmed )
                {
                    ExecuteSkill();
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
                var cooperativeAttackers = _candidateFinder.GetCooperativeAttackers( _targetSelector.AttackTargetCharaKeys );
                var options = ( cooperativeAttackers.Count > 0 )
                    ? new List<USE_SKILL_OPTION_TAG> { USE_SKILL_OPTION_TAG.COOPERATIVE, USE_SKILL_OPTION_TAG.QUEUE }
                    : new List<USE_SKILL_OPTION_TAG> { USE_SKILL_OPTION_TAG.EXECUTION,   USE_SKILL_OPTION_TAG.QUEUE };

                if( cooperativeAttackers.Count > 0 ) { ApplyCooperativeTotalDamagePreview(); }

                // 選択した攻撃範囲以外のタイル描画を全てクリアする
                _plOwner.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.MOVEABLE | TileMapType.ATTACKABLE );

                SetSendTransitionContext( new SkillUseOptionContext( options, cooperativeAttackers ) );
                _isWaitingForOptionResult = true;
                TransitState( ( int ) TransitTag.USE_SKILL_OPTION );
            }
            else
            {
                TryExecuteSkillWithKillConfirm();
            }

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            _targetSelector.TargetCharacter?.ResetRotationOrder();

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

        /// <summary>
        /// 対象が他キャラクターの攻撃予約対象であり、かつこの攻撃で倒してしまう場合は確認ダイアログを挟みます。
        /// 該当しない場合はそのままスキルを実行します。
        /// </summary>
        private void TryExecuteSkillWithKillConfirm()
        {
            if( IsLethalToReservedTarget( _targetSelector.TargetCharacter ) )
            {
                _isWaitingForKillConfirmResult = true;
                TransitState( ( int ) TransitTag.CONFIRM_KILL_RESERVED_TARGET );
                return;
            }

            ExecuteSkill();
        }

        private bool IsLethalToReservedTarget( Character target )
        {
            if( target == null || target.GetStatusRef.characterTag != CHARACTER_TAG.ENEMY ) { return false; }
            if( !_reservationQueue.HasReservationTargeting( target.GetCharacterKey() ) ) { return false; }

            return target.GetStatusRef.CurHP + target.BattleParams.TmpParam.TotalExpectedHpChange <= 0;
        }

        private void ExecuteSkill()
        {
            // 連携攻撃ではない通常攻撃・非連携スキルは、キル確認を挟む場合も含めここが実行確定のタイミングとなるため、SkillBoxUIを元の位置に戻す
            _presenter.RevertSkillBoxesFromSelection( ParameterWindowType.Left );

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

            var focusedTargetKey = _targetSelector.TargetCharacter?.GetCharacterKey() ?? CharacterKey.Invalid;

            // 攻撃対象全員分の予測ダメージを、フォーカス中かどうかに関わらず算出して保持する
            var targetHpChangeMap      = new Dictionary<CharacterKey, int>();
            var targetTotalHpChangeMap = new Dictionary<CharacterKey, int>();
            foreach( var targetKey in _targetSelector.AttackTargetCharaKeys )
            {
                var target = _btlRtnCtrl.BtlCharaCdr.GetCharacter( targetKey );
                if( target == null ) { continue; }

                var ( single, total ) = _btlRtnCtrl.BtlCharaCdr.CalculateExpectedHpChange( _plOwner, target );
                targetHpChangeMap[targetKey]      = single;
                targetTotalHpChangeMap[targetKey] = total;
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
                targetHpChangeMap,
                targetTotalHpChangeMap,
                _plOwner.GhostObj?.TileIndex ?? -1
            );

            _reservationQueue.Enqueue( data );
        }

        /// <summary>
        /// 連携攻撃確定前に、参加する全攻撃(自分自身の現在の攻撃 + 各連携攻撃者の予約)を
        /// 対象キャラクターごとに合算し、確認画面でのパラメータ表示(TotalExpectedHpChange)に反映します。
        /// 予約データが対象キャラクターごとの予測値を保持しているため、フォーカス対象以外への予測値も正しく集計されます。
        /// </summary>
        private void ApplyCooperativeTotalDamagePreview()
        {
            var totalDamageByTarget = new Dictionary<CharacterKey, int>();

            // 自分自身の現在の攻撃分。TmpParamの値はフォーカス対象以外は更新されていない可能性があるため、その場で全対象分を計算する
            foreach( var targetKey in _targetSelector.AttackTargetCharaKeys )
            {
                var target = _btlRtnCtrl.BtlCharaCdr.GetCharacter( targetKey );
                if( target == null ) { continue; }

                var ( _, total ) = _btlRtnCtrl.BtlCharaCdr.CalculateExpectedHpChange( _plOwner, target );
                totalDamageByTarget[targetKey] = total;
            }

            // 各連携攻撃者の予約分(対象キャラクターごとの予測値をそのまま合算できる)
            foreach( var reservation in _candidateFinder.GetCooperativeReservations( _targetSelector.AttackTargetCharaKeys ) )
            {
                foreach( var pair in reservation.TargetTotalExpectedHpChange )
                {
                    totalDamageByTarget.TryGetValue( pair.Key, out int current );
                    totalDamageByTarget[pair.Key] = current + pair.Value;
                }
            }

            foreach( var pair in totalDamageByTarget )
            {
                var target = _btlRtnCtrl.BtlCharaCdr.GetCharacter( pair.Key );
                if( target == null ) { continue; }

                int single = target.BattleParams.TmpParam.ExpectedHpChange;
                target.BattleParams.TmpParam.SetExpectedHpChange( single, pair.Value );
            }
        }

        private void ExecuteCooperativeSkill()
        {
            _isCooperativeSkill = true;

            var cooperativeAttackers = _candidateFinder.GetCooperativeAttackers( _targetSelector.AttackTargetCharaKeys );
            var entries = new List<CooperativeSkillEntry>( cooperativeAttackers.Count + 1 );

            foreach( var attacker in cooperativeAttackers )
            {
                if( !_reservationQueue.TryDequeueByAttackerKey( attacker.GetCharacterKey(), out var data ) ) { continue; }

                var target = ReservedSkillActionApplier.Apply( data, attacker, _stageCtrl, _btlRtnCtrl.BtlCharaCdr );

                // 行動終了の確定は、連携攻撃が完全に終わったタイミングで参加キャラクター全員分まとめて CooperativeSkillSequence.End() で行う

                entries.Add( _sequenceFcd.CreateCooperativeEntry( data.UseSkillID, attacker, target, new List<CharacterKey>( data.AttackTargetCharaKeys ) ) );
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

            entries.Add( _sequenceFcd.CreateCooperativeEntry( _useSkillID, _plOwner, _targetSelector.TargetCharacter, _targetSelector.AttackTargetCharaKeys ) );

            _sequenceFcd.RegistCooperativeSkillAction( entries );

            _phase = PlSkillActionPhase.PL_SKILL_ACTION_EXECUTE;
        }

        private void CleanupEnqueuedAction()
        {
            _plOwner.BattleParams.TmpParam.IsSkillQueued = true;
            _plOwner.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesAllType();
            _plOwner.ClearCommandHistory();

            // 移動を伴うスキルの着地予定地を予約し、実行されるまで他キャラクターが留まれないようにする
            // (解除は実際の実行時、DashSlashSA/JumpSlashSA等のEndActionで行われる)
            _stageCtrl.TileDataHdlr().ReserveTile( _plOwner.GhostObj?.TileIndex ?? -1 );

            _stageCtrl.SetActiveGridCursor( false );
            _stageCtrl.SetActiveTargetCursor( false );
            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );
        }

        private TargetingRangeContext BuildTargetingContext()
        {
            return new TargetingRangeContext { BtlRtnCtrl = _btlRtnCtrl, Presenter = _presenter, Owner = _plOwner, StageCtrl = _stageCtrl };
        }
    }
}
