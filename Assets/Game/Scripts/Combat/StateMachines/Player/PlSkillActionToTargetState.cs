using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

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

        private SkillID _useSkillID;
        private PlSkillActionPhase _phase;
        private Character _targetCharacter = null;

        public override void Init( object context )
        {
            base.Init( context );

            // 使用スキルを取得
            ReceiveContext( ref _useSkillID, context );

            // 使用スキルから攻撃可能範囲を決定
            _plOwner.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( _plOwner.BattleParams.TmpParam.CurrentTileIndex, _useSkillID );
            _plOwner.BattleLogic.ActionRangeCtrl.DrawAttackableRange();

            // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
            if( _stageCtrl.TileDataHdlr().CorrectAttackableTileIndexs( _plOwner.BattleLogic.ActionRangeCtrl, _btlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter( _plOwner, CHARACTER_TAG.ENEMY ) ) )
            {
                _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _plOwner );          // アタッカーキャラクターの設定
                _presenter.SetActiveActionResultExpect( true, ParameterWindowType.Left ); // アクション対象指定関連のUIを表示
            }

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
                    // グリッド上のキャラクターを取得
                    var prevTargetCharacter = _targetCharacter;
                    _targetCharacter        = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();

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
                    }

                    // 予測ダメージを適応する
                    _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( _plOwner, _targetCharacter );
                    break;
                default:
                    break;
            }

            return false;
        }

        public override object ExitState()
        {
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _plOwner );  // グリッドカーソルの位置をプレイヤーの位置に合わせる

            //死亡判定を通知
            Character diedCharacter = null;// _attackSequence.GetDiedCharacter();
            if( diedCharacter != null )
            {
                var key = new CharacterKey( diedCharacter.GetStatusRef.characterTag, diedCharacter.GetStatusRef.characterIndex );
                NorifyCharacterDied( key );
                diedCharacter.Dispose();    // 破棄
            }

            _stageCtrl.ClearGridCursorBind();                                             // アタッカーキャラクターの設定を解除
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
            _stageCtrl.SetGridCursorControllerActive( true );   // 選択グリッドを表示

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
                (GuideIcon.INFO,        "STATUS", CanAcceptInfo, new AcceptContextInput( AcceptInfo ), 0.0f, hashCode)
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
            return false;
        }

        protected override bool CanAcceptCancel()
        {
            return true;
        }

        protected override bool CanAcceptInfo()
        {
            if( !CanAcceptDefault() ) { return false; }

            // 自身以外のキャラクターが選択されていない場合は不可
            if( _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() == null || _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() == _plOwner )
            {
                return false;
            }

            return true;
        }

        protected override bool AcceptDirection( InputContext context )
        {
            bool isAcceptDirection = _stageCtrl.OperateGridCursorController( context.Cursor );

            /*
            if( isAcceptDirection )
            {
                var gridSelectChara = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
                _isActiveRightParamUI = ( gridSelectChara != null && gridSelectChara != _plOwner );
                if( _isActiveRightParamUI )
                {
                    _presenter.AssignCharacter( gridSelectChara, UI.ParameterWindowType.Right );
                }
                _presenter.SetActiveParamView( _isActiveRightParamUI, UI.ParameterWindowType.Right );
            }
            */

            return isAcceptDirection;
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
    }
}