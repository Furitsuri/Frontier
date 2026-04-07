using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
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
        private Character _targetCharacter              = null;
        private List<CharacterKey> _targetingCharaKeys  = new();
        private Action<Direction>[] _changeDirectionCallbacks;
        private readonly TileBitFlag[] CollectTileBitFlags = new TileBitFlag[( int )TargetingMode.NUM]
        {
            TileBitFlag.ATTACKABLE_TARGET_EXIST,                            // TargetingMode.Center
            TileBitFlag.TARGETABLE | TileBitFlag.ATTACKABLE_TARGET_EXIST,   // TargetingMode.Directional
            TileBitFlag.ATTACKABLE_TARGET_EXIST,                            // TargetingMode.All
        };
        private Func<Character, CHARACTER_TAG, Character>[] GetTargetCharacterCallbacks;

        public override void Init( object context )
        {
            base.Init( context );

            GetTargetCharacterCallbacks = new Func<Character, CHARACTER_TAG, Character>[( int )TargetingMode.NUM]
            {
                ( character, tag ) => character,                        // TargetingMode.Center
                _btlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter, // TargetingMode.Directional
                _btlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter, // TargetingMode.All
            };

            _targetCharacter = null;

            // 使用スキルを取得
            ReceiveContext( ref _useSkillID, context );

            _targetingMode  = SkillsData.data[( int ) _useSkillID].TargetingMode;
            _targetingValue = SkillsData.data[( int ) _useSkillID].TargetingValue;

            // 使用スキルから攻撃可能範囲を描画
            // MEMO : 攻撃範囲については前StateのPlSelectSkillStateで設定済みであるため、ここでは設定しないことに注意
            _plOwner.BattleLogic.ActionRangeCtrl.RefreshTargetingRange( _targetingMode, _stageCtrl.GetCurrentGridIndex(), _targetingValue );
            _plOwner.BattleLogic.ActionRangeCtrl.ReDrawAttackableRange();

            _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _plOwner );    // アタッカーキャラクターの設定

            // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
            if( _stageCtrl.TileDataHdlr().CollectAttackableTileIndicesWithFlag( _plOwner.BattleLogic.ActionRangeCtrl.ActionableTileMap.AttackableTileMap, CollectTileBitFlags[( int )_targetingMode] ) )
            {
                _stageCtrl.MoveGridCursorToAttackableTile( GetTargetCharacterCallbacks[( int )_targetingMode]( _plOwner, CHARACTER_TAG.ENEMY ) );
                _presenter.SetActiveActionResultExpect( true, ParameterWindowType.Left ); // アクション対象指定関連のUIを表示
            }

            _changeDirectionCallbacks = new Action<Direction>[( int )TargetingMode.NUM]
            {
                AcceptDirectionWithTargetingModeCenter,        // TargetingMode.Center
                AcceptDirectionWithTargetingModeDirectional,   // TargetingMode.Directional
                null,                                          // TargetingMode.Allは方向の概念がないため、コールバックは不要
            };

            _phase = PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID;
        }

        public override bool Update()
        {
            bool isActiveRightParameterView = ( null != _targetCharacter );
            _presenter.CharaParamView( ParameterWindowType.Right ).SetActive( isActiveRightParameterView );

            if( base.Update() )
            {
                return true;
            }

            switch( _phase )
            {
                case PlSkillActionPhase.PL_SKILL_ACTION_SELECT_GRID:
                    /*
                    // グリッド上のキャラクターを取得
                    var prevTargetCharacter = _targetCharacter;
                    _targetCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();

                    // 選択キャラクターが更新された場合はパラメータUIへの描画対象と、キャラクターの向きを更新
                    if( prevTargetCharacter != _targetCharacter )
                    {
                        var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Right );
                        _presenter.CharaParamView( ParameterWindowType.Right ).AssignCharacter( _targetCharacter, layerMaskIndex );

                        if( null != prevTargetCharacter )
                        {
                            prevTargetCharacter.GetTransformHandler.ResetRotationOrder();
                        }

                        var targetTileData = _stageCtrl.GetTileStaticData( _targetCharacter.BattleParams.TmpParam.CurrentTileIndex );
                        _plOwner.GetTransformHandler.RotateToPosition( targetTileData.CharaStandPos );
                        var attackerTileData = _stageCtrl.GetTileStaticData( _plOwner.BattleParams.TmpParam.CurrentTileIndex );
                        _targetCharacter.GetTransformHandler.RotateToPosition( attackerTileData.CharaStandPos );

                        _plOwner.BattleLogic.ActionRangeCtrl.RefreshTargetingRange( _targetingMode, -1, -1 );
                        _plOwner.BattleLogic.ActionRangeCtrl.ReDrawAttackableRange();
                    }

                    // 予測ダメージを適応する
                    _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( _plOwner, _targetCharacter );
                    */
                    break;
                default:
                    break;
            }

            return false;
        }

        public override object ExitState()
        {
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _plOwner );  // グリッドカーソルの位置をプレイヤーの位置に合わせる
            _stageCtrl.ClearGridCursorBind();                       // アタッカーキャラクターの設定を解除

            //死亡判定を通知
            Character diedCharacter = null;// _attackSequence.GetDiedCharacter();
            if( diedCharacter != null )
            {
                var key = new CharacterKey( diedCharacter.GetStatusRef.characterTag, diedCharacter.GetStatusRef.characterIndex );
                NorifyCharacterDied( key );
                diedCharacter.Dispose();    // 破棄
            }

            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );    // アクション対象指定関連のUIを非表示

            // 予測ダメージと使用スキルコスト見積もりをリセット
            if( null != _plOwner )
            {
                _plOwner.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            }
            if( null != _targetCharacter )
            {
                _targetCharacter.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            }

            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();       // タイルメッシュの描画をすべてクリア
            _stageCtrl.SetActiveGridCursor( true );   // 選択グリッドを表示

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
            LazyInject.GetOrCreate( ref _plOwner, () => _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player );
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
            return ( 0 < _targetingCharaKeys.Count );
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

        protected override bool CanAcceptSub1() { return CanAcceptConfirm(); }
        protected override bool CanAcceptSub2() { return CanAcceptConfirm(); }

        protected override bool AcceptDirection( InputContext context )
        {
            context.Cursor = _stageCtrl.ConvertDirectionDependOnCameraAngle( context.Cursor );
            if( Direction.NONE == context.Cursor ) { return false; }

            _changeDirectionCallbacks[( int ) _targetingMode]?.Invoke( context.Cursor );

            _plOwner.BattleLogic.ActionRangeCtrl.ReDrawAttackableRange();

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

            // ターゲット切り替え
            if( _stageCtrl.OperateTargetSelect( Direction.LEFT ) )
            {
                return true;
            }

            return true;
        }

        protected override bool AcceptSub2( InputContext context )
        {
            if( !base.AcceptSub2( context ) ) { return false; }

            // ターゲット切り替え
            if( _stageCtrl.OperateTargetSelect( Direction.RIGHT ) )
            {
                return true;
            }

            return true;
        }

        private void AcceptDirectionWithTargetingModeCenter( Direction dir )
        {
            _stageCtrl.OperateGridCursorController( dir );

            _targetingCharaKeys = _plOwner.BattleLogic.ActionRangeCtrl.RefreshTargetingRange( _targetingMode, _stageCtrl.GetCurrentGridIndex(), _targetingValue );
        }

        private void AcceptDirectionWithTargetingModeDirectional( Direction dir )
        {
            _plOwner.GetTransformHandler.OrderRotate( Quaternion.Euler( 0f, StageDirectionConverter.DirectionAngles[( int ) dir], 0f ) );

            _targetingCharaKeys = _plOwner.BattleLogic.ActionRangeCtrl.RefreshTargetingRange( _targetingMode, -1, _targetingValue );

            // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
            if( _stageCtrl.TileDataHdlr().CollectAttackableTileIndicesWithFlag( _plOwner.BattleLogic.ActionRangeCtrl.ActionableTileMap.AttackableTileMap, TileBitFlag.TARGETABLE ) )
            {
                _stageCtrl.MoveGridCursorToAttackableTile( _btlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter( _plOwner, CHARACTER_TAG.ENEMY ) );

                // _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _plOwner );          // アタッカーキャラクターの設定
                // _presenter.SetActiveActionResultExpect( true, ParameterWindowType.Left ); // アクション対象指定関連のUIを表示
            }
        }
    }
}