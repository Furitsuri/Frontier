using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System;
using System.Collections.Generic;
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
        }

        protected enum PlSkillActionPhase : int
        {
            PL_SKILL_ACTION_SELECT_GRID = 0,
            PL_SKILL_ACTION_EXECUTE,
            PL_SKILL_ACTION_END,
        }

        private int _targetingValue;
        private SkillID _useSkillID;
        private PlSkillActionPhase _phase;
        private TargetingMode _targetingMode;
        private Character _targetCharacter                  = null;
        private List<CharacterKey> _attackTargetCharaKeys   = null;
        private Action<Direction>[] _changeDirectionCallbacks;
        private Func<Character, List<CharacterKey>, Character>[] GetTargetCharacterCallbacks;
        private readonly TileBitFlag[] CollectTileBitFlags = new TileBitFlag[( int )TargetingMode.NUM]
        {
            TileBitFlag.ATTACKABLE_TARGET_EXIST,                            // TargetingMode.NORMAL_ATTACK
            TileBitFlag.TARGETABLE | TileBitFlag.ATTACKABLE_TARGET_EXIST,   // TargetingMode.CENTER
            TileBitFlag.TARGETABLE | TileBitFlag.ATTACKABLE_TARGET_EXIST,   // TargetingMode.DIRECTIONAL
            TileBitFlag.ATTACKABLE_TARGET_EXIST,                            // TargetingMode.ALL
        };

        [Inject] public PlSkillActionToTargetState( BattleRoutineController btlRtnCtrl )
        {
            _btlRtnCtrl = btlRtnCtrl;

            _attackTargetCharaKeys = new List<CharacterKey>();

            _changeDirectionCallbacks = new Action<Direction>[( int ) TargetingMode.NUM]
            {
                AcceptDirectionWithTargetingModeDirectional,    // TargetingMode.NORMAL_ATTACKはキャラクターの向きに依存するため、TargetingMode.Directionalと同じコールバックを使用
                AcceptDirectionWithTargetingModeCenter,         // TargetingMode.Center
                AcceptDirectionWithTargetingModeDirectional,    // TargetingMode.Directional
                null,                                           // TargetingMode.Allは方向の概念がないため、コールバックは不要
            };

            GetTargetCharacterCallbacks = new Func<Character, List<CharacterKey>, Character>[( int ) TargetingMode.NUM]
            {
                _btlRtnCtrl.BtlCharaCdr.GetNearestCharacter,            // TargetingMode.NORMAL_ATTACKはキャラクターの向きに依存するため、TargetingMode.Directionalと同じコールバックを使用
                _btlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter, // TargetingMode.Center
                _btlRtnCtrl.BtlCharaCdr.GetNearestCharacter,            // TargetingMode.Directional
                _btlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter, // TargetingMode.All
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

            _targetCharacter = null;

            // 使用スキルを取得
            ReceiveContext( ref _useSkillID, context );
            // アタッカーキャラクターの設定
            _stageCtrl.BindGridCursor( GridCursorState.ATTACK, _plOwner );

            _targetingMode  = SkillsData.data[( int ) _useSkillID].TargetingMode;
            _targetingValue = SkillsData.data[( int ) _useSkillID].TargetingValue;
            
            _changeDirectionCallbacks[( int ) _targetingMode]?.Invoke( _plOwner.GetTransformHandler.GetDirection() );

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
                    break;
                case PlSkillActionPhase.PL_SKILL_ACTION_END:
                    break;
                default:
                    break;
            }

            return false;
        }

        public override object ExitState()
        {
            OnExitStateAfterCombat( _plOwner, _targetCharacter );

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
                (new GuideIcon[] { GuideIcon.SUB1, GuideIcon.SUB2 }, "CHANGE\nCHARACTER", new EnableCallback[] { CanAcceptSub1, CanAcceptSub2 }, new IAcceptInputBase[] { new AcceptContextInput( AcceptSub1 ), new AcceptContextInput( AcceptSub2 ) }, 0.0f, hashCode)
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
        }

        protected override bool CanAcceptDirection()
        {
            if( !CanAcceptDefault() ) { return false; }

            return true;
        }

        protected override bool CanAcceptConfirm()
        {
            return ( 0 < _attackTargetCharaKeys.Count );
        }

        protected override bool CanAcceptCancel()
        {
            return true;
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

        protected override bool CanAcceptSub1() { return 1 < _attackTargetCharaKeys.Count; }
        protected override bool CanAcceptSub2() { return CanAcceptSub1(); }

        protected override bool AcceptDirection( InputContext context )
        {
            context.Cursor = _stageCtrl.ConvertDirectionDependOnCameraAngle( context.Cursor );
            if( Direction.NONE == context.Cursor ) { return false; }

            _changeDirectionCallbacks[( int ) _targetingMode]?.Invoke( context.Cursor );

            return true;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }



            return true;
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
            SetSendTransitionContext( _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() );

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

        private void AcceptDirectionWithTargetingModeCenter( Direction dir )
        {
            _stageCtrl.OperateGridCursorController( dir );

            _plOwner.BattleLogic.ActionRangeCtrl.RefreshTargetingRange( _targetingMode, _stageCtrl.GetCurrentGridIndex(), _targetingValue );
        }

        private void AcceptDirectionWithTargetingModeDirectional( Direction dir )
        {
            var actionRangeCtrl = _plOwner.BattleLogic.ActionRangeCtrl;
            var targetingMode   = ( int )_targetingMode;

            _plOwner.GetTransformHandler.OrderRotate( Quaternion.Euler( 0f, StageDirectionConverter.DirectionAngles[( int ) dir], 0f ) );
            actionRangeCtrl.RefreshTargetingRange( _targetingMode, -1, _targetingValue );
            _attackTargetCharaKeys = actionRangeCtrl.GetAttackTargetCharacterKeys();

            // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
            if( 0 < _attackTargetCharaKeys.Count )
            {
                _targetCharacter = GetTargetCharacterCallbacks[targetingMode]( _plOwner, _attackTargetCharaKeys );
                Debug.Assert( _targetCharacter != null, "攻撃可能なグリッドが存在する場合、必ずターゲットキャラクターが存在するはずですが、nullが返されました。" );

                _stageCtrl.MoveGridCursorToAttackableTile( _targetCharacter );
                _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( _plOwner, _targetCharacter );    // 予測ダメージを適用する
                _presenter.SetActiveActionResultExpect( true, ParameterWindowType.Left );   // アクション対象指定関連のUIを表示
            }
            else
            {
                _targetCharacter = null;

                // 攻撃可能なグリッドがない場合はカーソル位置(カメラ位置)をプレイヤーに合わせる
                _stageCtrl.ApplyCurrentGrid2CharacterTile( _plOwner );
                _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );
            }
        }

        private void AcceptSub( Direction dir )
        {
            // ターゲット切り替え
            if( _stageCtrl.OperateTargetSelect( dir ) )
            {
                _targetCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
                _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( _plOwner, _targetCharacter );    // 予測ダメージを適用する
            }
        }
    }
}