using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
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

        private delegate void ChangeDirectionCallback( TargetingRangeContext context, Direction dir, bool isWithMove, ref int currentRange, int maxRange );
        private delegate void RefreshTargetCallback( TargetingRangeContext context, bool isMovingSkill, ref List<CharacterKey> attackTargetCharaKeys, ref Character targetCharacter );
        private delegate bool TryAdjustRangeCallback( TargetingRangeContext context, int step, ref int currentRange, int maxRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter );
        private delegate int GetGhostTileIndexCallback();

        private int _currentRange;
        private int _maxRange;
        private bool _isAdjustableRange;
        private bool _isMovingSkill;
        private bool _isWaitingForOptionResult;
        private bool _isSkillQueued;
        private SkillID _useSkillID;
        private PlSkillActionPhase _phase;
        private TargetingMode _targetingMode;
        private Character _targetCharacter                  = null;
        private SequenceFacade _sequenceFcd                         = null;
        private SkillActionReservationQueue _reservationQueue       = null;
        private List<CharacterKey> _attackTargetCharaKeys           = null;
        private ChangeDirectionCallback[] _changeDirectionCallbacks;
        private RefreshTargetCallback[] _refreshFocusTargetCallbacks;
        private TryAdjustRangeCallback[] _tryAdjustRangeCallbacks;

        [Inject] public PlSkillActionToTargetState( BattleRoutineController btlRtnCtrl, SequenceFacade sequenceFcd, SkillActionReservationQueue reservationQueue )
        {
            _btlRtnCtrl         = btlRtnCtrl;
            _sequenceFcd        = sequenceFcd;
            _reservationQueue   = reservationQueue;

            _attackTargetCharaKeys = new List<CharacterKey>();

            _changeDirectionCallbacks = new ChangeDirectionCallback[( int ) TargetingMode.NUM]
            {
                NormalAttackTargetingRange.AcceptDirection,     // TargetingMode.NORMAL_ATTACK
                PartOfRangeTargetingRange.AcceptDirection,      // TargetingMode.PART_OF_RANGE
                DirectionalTargetingRange.AcceptDirection,      // TargetingMode.DIRECTIONAL
                AllTargetingRange.AcceptDirection,              // TargetingMode.ALL
            };

            _refreshFocusTargetCallbacks = new RefreshTargetCallback[( int ) TargetingMode.NUM]
            {
                NormalAttackTargetingRange.RefreshFocusTarget,       // TargetingMode.NORMAL_ATTACK
                PartOfRangeTargetingRange.RefreshFocusTarget,        // TargetingMode.PART_OF_RANGE
                DirectionalTargetingRange.RefreshFocusTarget,        // TargetingMode.DIRECTIONAL
                AllTargetingRange.RefreshFocusTarget,                // TargetingMode.ALL
            };

            _tryAdjustRangeCallbacks = new TryAdjustRangeCallback[( int ) TargetingMode.NUM]
            {
                NormalAttackTargetingRange.TryAdjustRange,      // TargetingMode.NORMAL_ATTACK
                PartOfRangeTargetingRange.TryAdjustRange,       // TargetingMode.PART_OF_RANGE
                DirectionalTargetingRange.TryAdjustRange,       // TargetingMode.DIRECTIONAL
                AllTargetingRange.TryAdjustRange,               // TargetingMode.ALL
            };
        }

        /// <summary>
        /// 初期化します
        /// MEMO : 攻撃範囲については前StateのPlSelectSkillStateで設定済みであるため、ここでは設定しないことに注意
        /// </summary>
        /// <param name="context"></param>
        public override void Init( object context )
        {
            base.Init( context );

            _targetCharacter          = null;
            _isWaitingForOptionResult = false;
            _isSkillQueued            = false;

            // 使用スキルを取得
            ReceiveContext( ref _useSkillID, context );
            // アタッカーキャラクターの設定
            _stageCtrl.BindGridCursor( GridCursorState.ATTACK, _plOwner );

            var skillData       = SkillsData.data[( int ) _useSkillID];
            _targetingMode      = skillData.TargetingMode;
            // DIRECTIONALは自身の向きに沿った直線状の範囲すべてが攻撃対象範囲となるため、RangeValueをそのまま適用
            _maxRange = ( TargetingMode.DIRECTIONAL != _targetingMode ) ? skillData.TargetingRange : skillData.RangeValue;
            _currentRange       = _maxRange;
            _isAdjustableRange  = skillData.IsAdjustableRange;
            _isMovingSkill      = skillData.IsMovingSkill;
            
            var targetingContext = new TargetingRangeContext { BtlRtnCtrl = _btlRtnCtrl, Presenter = _presenter, Owner = _plOwner, StageCtrl = _stageCtrl };

            _plOwner.BattleLogic.ActionRangeCtrl.RefreshTargetableRange( _targetingMode, true, _isMovingSkill, _plOwner.BattleParams.TmpParam.CurrentTileIndex, _currentRange );
            _refreshFocusTargetCallbacks[( int ) _targetingMode]?.Invoke( targetingContext, _isMovingSkill, ref _attackTargetCharaKeys, ref _targetCharacter );

            _phase = PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID;
        }

        public override bool Update()
        {
            bool isActiveRightParameterView = ( null != _targetCharacter );
            _presenter.CharaParamView( ParameterWindowType.Right ).SetActive( isActiveRightParameterView );
            if( isActiveRightParameterView )
            {
                var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Right );
                _presenter.CharaParamView( ParameterWindowType.Right ).AssignCharacter( _targetCharacter, layerMaskIndex );
            }

            if( base.Update() )
            {
                return true;
            }

            switch( _phase )
            {
                case PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID:
                    break;
                case PlSkillActionPhase.PL_SKILL_ACTION_EXECUTE:
                    _phase = PlSkillActionPhase.PL_SKILL_ACTION_END;
                    break;
                case PlSkillActionPhase.PL_SKILL_ACTION_END:
                    // スキルコマンドを選択不可にする
                    _plOwner.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.SKILL, true );
                    // スキル使用後は移動やその他のコマンドも使用できないようにするため行動履歴をクリア
                    _plOwner.ClearCommandHistory();
                    // コマンド選択に戻る
                    Back();
                    return true;
                default:
                    break;
            }

            return false;
        }

        public override object ExitState()
        {
            OnExitStateAfterCombat( _plOwner, _targetCharacter );

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

            // 入力ガイドを登録
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
            // グリッドカーソルで選択中のプレイヤーを取得
            var selectCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            _plOwner            = _btlRtnCtrl.BtlCharaCdr.GetPlayer( selectCharacter.GetCharacterKey() );
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            // パラメータビューにキャラクターを割り当て
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
                        // TODO: 連携処理（次フェーズで実装）
                        break;
                }
            }
        }

        protected override bool CanAcceptDefault()
        {
            if( _phase != PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID ) { return false; }
            return base.CanAcceptDefault();
        }

        protected override bool CanAcceptDirection()
        {
            if( !CanAcceptDefault() ) { return false; }

            return true;
        }

        protected override bool CanAcceptConfirm()
        {
            if( _phase != PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID ) { return false; }
            return IsExecutableAttack();
        }

        protected override bool CanAcceptCancel()
        {
            return _phase == PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID;
        }

        protected override bool CanAcceptInfo()
        {
            if( !CanAcceptDefault() ) { return false; }

            // 自身以外のキャラクターが選択されていない場合は不可
            var selectCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( selectCharacter == null || selectCharacter == _plOwner )
            {
                return false;
            }

            return true;
        }

        protected override bool CanAcceptSub1() { return _phase == PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID && 1 < _attackTargetCharaKeys.Count; }
        protected override bool CanAcceptSub2() { return CanAcceptSub1(); }

        protected override bool CanAcceptSub3() => _phase == PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID && _isAdjustableRange && 1 < _currentRange;
        protected override bool CanAcceptSub4() => _phase == PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID && _isAdjustableRange && _currentRange < _maxRange;

        protected override bool AcceptDirection( InputContext context )
        {
            context.Cursor = _stageCtrl.ConvertDirectionDependOnCameraAngle( context.Cursor );
            if( Direction.NONE == context.Cursor ) { return false; }

            var targetingContext = new TargetingRangeContext { BtlRtnCtrl = _btlRtnCtrl, Presenter = _presenter, Owner = _plOwner, StageCtrl = _stageCtrl };
            _changeDirectionCallbacks[( int ) _targetingMode]?.Invoke( targetingContext, context.Cursor, _isMovingSkill, ref _currentRange, _maxRange );
            _refreshFocusTargetCallbacks[( int ) _targetingMode]?.Invoke( targetingContext, _isMovingSkill, ref _attackTargetCharaKeys, ref _targetCharacter );

            return true;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            if( SkillsData.data[( int ) _useSkillID].IsCooperative )
            {
                var cooperativeAttackers = FindCooperativeAttackers();
                var options = ( cooperativeAttackers.Count > 0 )
                    ? new List<USE_SKILL_OPTION_TAG> { USE_SKILL_OPTION_TAG.COOPERATIVE, USE_SKILL_OPTION_TAG.QUEUE }
                    : new List<USE_SKILL_OPTION_TAG> { USE_SKILL_OPTION_TAG.EXECUTION,   USE_SKILL_OPTION_TAG.QUEUE };

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

        /// <summary>
        /// キュー内から現在の攻撃対象と重複するターゲットを持つアクションの攻撃者リストを返します
        /// </summary>
        private List<Character> FindCooperativeAttackers()
        {
            var result = new List<Character>();
            foreach( var reservation in _reservationQueue.GetAll() )
            {
                bool hasCommonTarget = reservation.AttackTargetCharaKeys.Any( key => _attackTargetCharaKeys.Contains( key ) );
                if( !hasCommonTarget ) { continue; }

                var attacker = _btlRtnCtrl.BtlCharaCdr.GetCharacter( reservation.AttackerKey );
                if( attacker != null && !result.Contains( attacker ) )
                {
                    result.Add( attacker );
                }
            }
            return result;
        }

        private void ExecuteSkill()
        {
            // アクションゲージを消費
            _plOwner.BattleLogic.ConsumeActionGaugeForSkill();
            if( null != _targetCharacter )
            {
                _targetCharacter.BattleLogic.ConsumeActionGauge();
            }

            _stageCtrl.SetActiveGridCursor( false );                                     // 選択グリッドを一時非表示
            _stageCtrl.SetActiveTargetCursor( false );                                   // ターゲットカーソルを一時非表示
            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );   // アクション対象指定関連のUIを非表示
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();                                // タイルメッシュの描画をすべてクリア

            // 自己バフスキルの登録(バフスキルが使用されていれば使用可能スキルを更新)
            if( _plOwner.BattleLogic.RegistSelfBuffSequences() )
            {
                _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
            }

            _sequenceFcd.RegistSkillAction( _plOwner, _targetCharacter, _useSkillID, _attackTargetCharaKeys );   // スキルシーケンスの開始

            _phase = PlSkillActionPhase.PL_SKILL_ACTION_EXECUTE;
        }

        private void EnqueueSkillAction()
        {
            _plOwner.BattleParams.TmpParam.AssignExpectedHpChange( out int attackerHpChange, out int attackerTotalHpChange );

            int targetHpChange = 0, targetTotalHpChange = 0;
            var focusedTargetKey = CharacterKey.Invalid;
            if( null != _targetCharacter )
            {
                _targetCharacter.BattleParams.TmpParam.AssignExpectedHpChange( out targetHpChange, out targetTotalHpChange );
                focusedTargetKey = _targetCharacter.GetCharacterKey();
            }

            var data = new SkillActionReservationData(
                _plOwner.GetCharacterKey(),
                _plOwner.BattleParams.TmpParam.CurrentTileIndex,
                _plOwner.BattleParams.TmpParam.IsSkillsToggledON,
                _plOwner.BattleParams.TmpParam.ActGaugeConsumption,
                _useSkillID,
                _targetingMode,
                _currentRange,
                _maxRange,
                _isAdjustableRange,
                _isMovingSkill,
                _attackTargetCharaKeys,
                focusedTargetKey,
                attackerHpChange,
                attackerTotalHpChange,
                targetHpChange,
                targetTotalHpChange,
                _plOwner.GhostObj?.TileIndex ?? -1
            );

            _reservationQueue.Enqueue( data );
        }

        private void CleanupEnqueuedAction()
        {
            _plOwner.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.SKILL, true );
            _plOwner.ClearCommandHistory();

            _stageCtrl.SetActiveGridCursor( false );                                     // 選択グリッドを一時非表示
            _stageCtrl.SetActiveTargetCursor( false );                                   // ターゲットカーソルを一時非表示
            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );   // アクション対象指定関連のUIを非表示
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();                                // タイルメッシュの描画をすべてクリア
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            // 攻撃対象キャラクターの向きをリセット
            if( null != _targetCharacter )
            {
                _targetCharacter.GetTransformHandler.ResetRotationOrder();
            }

            return true;
        }

        /// <summary>
        /// 情報キー入力時、ステータス画面へ遷移します
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            // ステータス表示ステートに対象キャラクターを渡す
            SetSendTransitionContext( _btlRtnCtrl.BtlCharaCdr.GetTargetCharacter() );

            TransitState( ( int ) TransitTag.CHARACTER_STATUS );

            return true;
        }

        protected override bool AcceptSub1( InputContext context )
        {
            if( !base.AcceptSub1( context ) ) { return false; }

            AcceptSub( Direction.LEFT );

            return true;
        }

        protected override bool AcceptSub2( InputContext context )
        {
            if( !base.AcceptSub2( context ) ) { return false; }

            AcceptSub( Direction.RIGHT );

            return true;
        }

        protected override bool AcceptSub3( InputContext context )
        {
            if( !base.AcceptSub3( context ) ) { return false; }

            var targetingContext = new TargetingRangeContext { BtlRtnCtrl = _btlRtnCtrl, Presenter = _presenter, Owner = _plOwner, StageCtrl = _stageCtrl };
            var callback = _tryAdjustRangeCallbacks[( int ) _targetingMode];
            return callback != null && callback( targetingContext, -1, ref _currentRange, _maxRange, _isMovingSkill, ref _attackTargetCharaKeys, ref _targetCharacter );
        }

        protected override bool AcceptSub4( InputContext context )
        {
            if( !base.AcceptSub4( context ) ) { return false; }

            var targetingContext = new TargetingRangeContext { BtlRtnCtrl = _btlRtnCtrl, Presenter = _presenter, Owner = _plOwner, StageCtrl = _stageCtrl };
            var callback = _tryAdjustRangeCallbacks[( int ) _targetingMode];
            return callback != null && callback( targetingContext, +1, ref _currentRange, _maxRange, _isMovingSkill, ref _attackTargetCharaKeys, ref _targetCharacter );
        }

        private void AcceptSub( Direction dir )
        {
            // ターゲット切り替え
            if( _stageCtrl.OperateTargetSelect( dir ) )
            {
                _targetCharacter = _btlRtnCtrl.BtlCharaCdr.GetTargetCharacter();
                _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( _plOwner, _targetCharacter );    // 予測ダメージを適用する
            }
        }

        private bool IsExecutableAttack()
        {
            return 0 < _attackTargetCharaKeys.Count;
        }
    }
}