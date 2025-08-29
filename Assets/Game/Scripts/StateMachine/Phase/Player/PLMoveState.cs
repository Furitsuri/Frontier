using Frontier.Combat;
using Frontier.Stage;
using Frontier.Entities;
using Frontier;
using System.Collections.Generic;
using UnityEngine;
using static Constants;
using Unity.VisualScripting;
using static Frontier.Stage.StageController;

namespace Frontier
{
    public class PlMoveState : PlPhaseStateBase
    {
        private enum PlMovePhase
        {
            PL_MOVE_SELECT_GRID = 0,
            PL_MOVE_EXECUTE,
            PL_MOVE,
            PL_MOVE_END,
        }

        const int TransitAttackStateValue = 0;
        private PlMovePhase _phase = PlMovePhase.PL_MOVE_SELECT_GRID;
        private int _departGridIndex = -1;
        private List<(int routeIndexs, int routeCost)> _movePathList = new List<(int, int)>(Constants.DIJKSTRA_ROUTE_INDEXS_MAX_NUM);

        override public void Init()
        {
            base.Init();

            // 攻撃が終了している場合(移動遷移中に直接攻撃を行った場合)
            if( _selectPlayer.Params.TmpParam.IsEndCommand( Command.COMMAND_TAG.ATTACK ) )
            {
                _phase = PlMovePhase.PL_MOVE_END;
                return;
            }
            else _phase = PlMovePhase.PL_MOVE;

            _departGridIndex = _selectPlayer.PrevMoveInformaiton.tmpParam.gridIndex;

            // 移動開始前の情報を保存
            var param = _selectPlayer.Params.CharacterParam;

            // キャラクターの現在の位置情報を保持
            StageController.Footprint footprint = new StageController.Footprint();
            footprint.gridIndex = _selectPlayer.Params.TmpParam.GetCurrentGridIndex();
            footprint.rotation  = _selectPlayer.transform.rotation;
            _stageCtrl.LeaveFootprint(footprint);
            _stageCtrl.BindGridCursorControllerState( GridCursorController.State.MOVE, _selectPlayer);

            // 移動可能情報を登録及び表示
            bool isAttackable = !_selectPlayer.Params.TmpParam.IsEndCommand( Command.COMMAND_TAG.ATTACK );
            _stageCtrl.RegistMoveableInfo(_departGridIndex, param.moveRange, param.attackRange, param.characterIndex, param.characterTag, isAttackable);
            _stageCtrl.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRange);
        }

        override public bool Update()
        {
            var stageGrid = _stageCtrl;

            if ( base.Update() )
            {
                // キャラクターのグリッドの位置に選択グリッドの位置を戻す
                stageGrid.FollowFootprint(_selectPlayer);

                return true;
            }

            switch( _phase )
            {
                case PlMovePhase.PL_MOVE:
                    // 移動目的座標の更新
                    GridInfo info;
                    var curGridIndex    = stageGrid.GetCurrentGridIndex();
                    var plGridIndex     = _selectPlayer.Params.TmpParam.GetCurrentGridIndex();
                    stageGrid.FetchCurrentGridInfo(out info);

                    // 移動更新
                    _selectPlayer.UpdateMove(curGridIndex, info);
                    break;
                case PlMovePhase.PL_MOVE_END:
                    // 移動したキャラクターの移動コマンドを選択不可にする
                    _selectPlayer.Params.TmpParam.SetEndCommandStatus(Command.COMMAND_TAG.MOVE, true);
                    // コマンド選択に戻る
                    Back();

                    return true;
            }

            return (0 <= TransitIndex);
        }

        override public void ExitState()
        {
            _stageCtrl.SetGridCursorControllerActive(true); // 選択グリッドを表示
            _stageCtrl.UpdateGridInfo();                    // ステージグリッド上のキャラ情報を更新
            _stageCtrl.ClearGridMeshDraw();                 // グリッド状態の描画をクリア

			// 直接遷移しない場合は操作対象データをリセット
			if ( TransitIndex != TransitAttackStateValue ) _stageCtrl.ClearGridCursroBind();

			base.ExitState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR,  "MOVE",     CanAcceptDirection, new AcceptDirectionInput(AcceptDirection),  GRID_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM,     "DECISION", CanAcceptConfirm, new AcceptBooleanInput(AcceptConfirm), 0.0f, hashCode),
                (GuideIcon.CANCEL,      "BACK",     CanAcceptDefault, new AcceptBooleanInput(AcceptCancel), 0.0f, hashCode)
             );
        }

        /// <summary>
        /// 決定入力受付の可否を判定します
        /// </summary>
        /// <returns>決定入力受付の可否</returns>
        override protected bool CanAcceptConfirm()
        {
            if (!CanAcceptDefault()) return false;

            GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);
            if (info.estimatedMoveRange < 0)
            {
                // 敵対勢力が存在している場合は直接攻撃を可能とするためにtrueを返す
                if ( CanAttackOnMove( in info ) ) return true;

                return false;  // 移動不可地点であれば不可
            }

            // 移動フェーズでない場合は終了
            if (PlMovePhase.PL_MOVE == _phase) return true;

            return false;
        }

        /// <summary>
        /// 方向入力受付の可否を判定します
        /// </summary>
        /// <returns>方向入力受付の可否</returns>
        override protected bool CanAcceptDirection()
        {
            if (!CanAcceptDefault()) return false;

            // 移動フェーズでない場合、または移動入力受付が不可能である場合は不可
            if ( PlMovePhase.PL_MOVE == _phase ) return true;

            return false;
        }

        /// <summary>
        /// 方向入力を受け取り、キャラクターを操作します
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力によってキャラクター移動が行われたか</returns>
        override protected bool AcceptDirection(Direction dir)
        {
            return _stageCtrl.OperateGridCursorController( dir );
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>決定入力実行の有無</returns>
        override protected bool AcceptConfirm(bool isInput)
        {
            if (!isInput) { return false; }

            GridInfo info;
            var curGridIndex = _stageCtrl.GetCurrentGridIndex();
            var plGridIndex = _selectPlayer.Params.TmpParam.GetCurrentGridIndex();
            _stageCtrl.FetchCurrentGridInfo(out info);

            // 出発地点と同一グリッドであれば戻る
            if (curGridIndex == _departGridIndex)
            {
                Back();
            }
            // 敵キャラクターが存在している場合は攻撃へ遷移
            else if( Methods.CheckBitFlag( info.flag, BitFlag.ENEMY_EXIST ) )
            {
                TransitIndex = TransitAttackStateValue;
            }
            // 敵キャラクター意外が存在していないことを確認
            else if (0 == (info.flag & (BitFlag.PLAYER_EXIST | BitFlag.OTHER_EXIST)))
            {
                _phase = PlMovePhase.PL_MOVE_END;
            }

            return true;
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力</param>
        /// <returns>キャンセル入力実行の有無</returns>
        override protected bool AcceptCancel(bool isCancel)
        {
            if( base.AcceptCancel(isCancel) )
            {
                // 巻き戻しを行う
                Rewind();

                return true;
            }

            return false;
        }

        /// <summary>
        /// 移動中(現在のステート中)に攻撃へと直接遷移出来るか否かを取得します
        /// </summary>
        /// <param name="info">グリッド情報</param>
        /// <returns>直接遷移の可否</returns>
        private bool CanAttackOnMove( in GridInfo info )
        {
            if( info.estimatedMoveRange != TILE_ON_OPPONENT_VALUE ) return false;

            // 現在位置と指定位置の差が攻撃レンジ以内であることが条件
            ( int, int ) ranges = _stageCtrl.CalcurateRanges( _selectPlayer.Params.TmpParam.gridIndex,  _stageCtrl.GetCurrentGridIndex());

            return ranges.Item1 + ranges.Item2 <= _selectPlayer.Params.CharacterParam.attackRange;
        }
    }
}