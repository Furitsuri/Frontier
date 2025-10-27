using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Constants;

namespace Frontier.StateMachine
{
    public class PlMoveState : PlPhaseStateBase
    {
        private enum PlMovePhase
        {
            PL_MOVE = 0,
            PL_MOVE_RESERVE_END,
            PL_MOVE_END,
        }

        const int TransitAttackStateValue   = 0;
        private PlMovePhase _phase          = PlMovePhase.PL_MOVE;
        private int _departTileIndex        = -1;

        override public void Init()
        {
            base.Init();

            // 攻撃が終了している場合(移動遷移中に直接攻撃を行った場合)
            if( _plOwner.Params.TmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) )
            {
                _phase = PlMovePhase.PL_MOVE_END;
                return;
            }
            else { _phase = PlMovePhase.PL_MOVE; }

            _departTileIndex = _plOwner.PrevMoveInformaiton.tmpParam.gridIndex;
            _stageCtrl.BindToGridCursor( GridCursorState.MOVE, _plOwner );

            // 移動可能情報を登録及び表示
            int atkRange            = !_plOwner.Params.TmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) ? _plOwner.Params.CharacterParam.attackRange : 0;
            var param               = _plOwner.Params.CharacterParam;
            float dprtTileHeight    = _stageCtrl.GetTileStaticData( _departTileIndex ).Height;
            _plOwner.ActionRangeCtrl.SetupActionableRangeData( _departTileIndex, dprtTileHeight );
            _plOwner.ActionRangeCtrl.DrawActionableRange();
        }

        override public bool Update()
        {
            if( base.Update() )
            {
                // キャラクターのグリッドの位置に選択グリッドの位置を戻す
                _stageCtrl.FollowFootprint( _plOwner );

                return true;
            }

            switch( _phase )
            {
                case PlMovePhase.PL_MOVE:
                    SetupMovePath();
                    _plOwner.UpdateMovePath();
                    break;

                case PlMovePhase.PL_MOVE_RESERVE_END:
                    // 移動完了後に終了へ移行
                    if( _plOwner.UpdateMovePath( CHARACTER_MOVE_HIGH_SPEED_RATE ) )
                    {
                        _phase = PlMovePhase.PL_MOVE_END;
                    }
                    break;

                case PlMovePhase.PL_MOVE_END:
                    // 移動したキャラクターの移動コマンドを選択不可にする
                    _plOwner.Params.TmpParam.SetEndCommandStatus( COMMAND_TAG.MOVE, true );
                    Back();     // コマンド選択に戻る

                    return true;
            }

            return ( 0 <= TransitIndex );
        }

        override public void ExitState()
        {
            _stageCtrl.SetGridCursorControllerActive( true );   // 選択グリッドを表示
            _stageCtrl.ClearTileMeshDraw();                     // グリッド状態の描画をクリア

            // 攻撃に直接遷移しない場合のみに限定される処理
            if( !IsTransitAttackOnMoveState() )
            {
                _stageCtrl.ClearGridCursroBind();           // 操作対象データをリセット
            }

            base.ExitState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR,  "MOVE",     CanAcceptDirection, new AcceptDirectionInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM,     "DECISION", CanAcceptConfirm, new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
                (GuideIcon.CANCEL,      "BACK",     CanAcceptDefault, new AcceptBooleanInput( AcceptCancel ), 0.0f, hashCode)
             );
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        override protected void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            _plOwner = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }

        /// <summary>
        /// 決定入力受付の可否を判定します
        /// </summary>
        /// <returns>決定入力受付の可否</returns>
        override protected bool CanAcceptConfirm()
        {
            if( !CanAcceptDefault() ) { return false; }

            if( PlMovePhase.PL_MOVE != _phase ) { return false; }     // 移動フェーズでない場合は終了

            // 移動不可の地点であっても、敵対勢力が存在しており自身の攻撃レンジ以内の場合にはtrueを返す
            int currentIndex = _stageCtrl.GetCurrentGridIndex();
            if( CanAttackOnMove( _plOwner.ActionRangeCtrl.ActionableTileMap.GetAttackableTile( currentIndex ) ) ) { return true; }
            // それ以外は留まることが可能かを確認
            else { return _plOwner.ActionRangeCtrl.MovePathHdlr.CanStandOnTile( _plOwner.ActionRangeCtrl.ActionableTileMap.GetMoveableTile( currentIndex ) ); }
        }

        /// <summary>
        /// 方向入力受付の可否を判定します
        /// </summary>
        /// <returns>方向入力受付の可否</returns>
        override protected bool CanAcceptDirection()
        {
            if( !CanAcceptDefault() ) { return false; }

            // 移動フェーズでない場合、または移動入力受付が不可能である場合は不可
            if( PlMovePhase.PL_MOVE == _phase ) return true;

            return false;
        }

        /// <summary>
        /// 方向入力を受け取り、キャラクターを操作します
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力によってキャラクター移動が行われたか</returns>
        override protected bool AcceptDirection( Direction dir )
        {
            return _stageCtrl.OperateGridCursorController( dir );
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>決定入力実行の有無</returns>
        override protected bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) { return false; }

            var currentIndex            = _stageCtrl.GetCurrentGridIndex();
            TileDynamicData tileData    = _plOwner.ActionRangeCtrl.ActionableTileMap.GetAttackableTile( currentIndex );

            // 出発地点と同一グリッドであれば戻る
            if( currentIndex == _departTileIndex )
            {
                Back();

                return true;
            }
            // 攻撃可能なキャラクターが存在している場合は攻撃へ遷移
            else if( null != tileData && Methods.CheckBitFlag( tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
            {
                TransitAttackOnMoveState();

                return true;
            }

            _phase = PlMovePhase.PL_MOVE_RESERVE_END;

            return true;
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力</param>
        /// <returns>キャンセル入力実行の有無</returns>
        override protected bool AcceptCancel( bool isCancel )
        {
            if( base.AcceptCancel( isCancel ) )
            {
                // 巻き戻しを行う
                Rewind();

                return true;
            }

            return false;
        }

        /// <summary>
        /// 移動中攻撃に遷移します
        /// </summary>
        private void TransitAttackOnMoveState()
        {
            TransitIndex = TransitAttackStateValue;
        }

        /// <summary>
        /// 移動するパスを作成します
        /// </summary>
        private void SetupMovePath()
        {
            int departingTileIndex      = _plOwner.Params.TmpParam.gridIndex;
            int destinationTileIndex    = _stageCtrl.GetCurrentGridIndex();
            MovePathHandler pathHdlr    = _plOwner.ActionRangeCtrl.MovePathHdlr;
            bool isEndPathTrace         = pathHdlr.IsEndPathTrace();

            // 現在のパストレースが終了していない場合は、直近のwaypointを出発地点にする
            if( !isEndPathTrace )
            {
                departingTileIndex = pathHdlr.GetFocusedWaypointIndex();
            }

            _plOwner.ActionRangeCtrl.FindActuallyMovePath( departingTileIndex, destinationTileIndex, _plOwner.Params.CharacterParam.jumpForce, _plOwner.TileCostTable, isEndPathTrace );
        }

        /// <summary>
        /// 移動中攻撃に遷移しているかどうかを取得します
        /// </summary>
        /// <returns>遷移の有無</returns>
        private bool IsTransitAttackOnMoveState()
        {
            return ( TransitIndex == TransitAttackStateValue );
        }

        /// <summary>
        /// 移動中(現在のステート中)に攻撃へと直接遷移出来るか否かを取得します
        /// </summary>
        /// <param name="info">グリッド情報</param>
        /// <returns>直接遷移の可否</returns>
        private bool CanAttackOnMove( in TileDynamicData tileData )
        {
            if( null == tileData ) { return false; }

            if( !Methods.CheckBitFlag( tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) ) { return false; }

            // 現在位置と指定位置の差が攻撃レンジ以内であることが条件
            (int, int) ranges = _stageCtrl.CalcurateRanges( _plOwner.Params.TmpParam.gridIndex, _stageCtrl.GetCurrentGridIndex() );

            return ranges.Item1 + ranges.Item2 <= _plOwner.Params.CharacterParam.attackRange;
        }
    }
}