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

        private PlMovePhase _phase = PlMovePhase.PL_MOVE_SELECT_GRID;
        private int _departGridIndex = -1;
        private List<(int routeIndexs, int routeCost)> _movePathList = new List<(int, int)>(Constants.DIJKSTRA_ROUTE_INDEXS_MAX_NUM);

        override public void Init()
        {
            base.Init();

            _inputFcd.RegisterInputCodes<Constants.Direction>(
                ((GuideIcon.ALL_CURSOR, "Move", CanAcceptInputDirection, DIRECTION_INPUT_INTERVAL), AcceptDirectionInput)
             );

            _inputFcd.RegisterInputCodes<bool>(
                ((GuideIcon.CONFIRM, "Decision", CanAcceptInputConfirm, 0.0f), AcceptConfirmInput),
                ((GuideIcon.CANCEL, "Back", CanAcceptInputDefault, 0.0f), AcceptRevertInput)
             );

            _phase = PlMovePhase.PL_MOVE;
            _departGridIndex = _stageCtrl.GetCurrentGridIndex();

            // 現在選択中のキャラクター情報を取得して移動範囲を表示
            _selectPlayer = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            DebugUtils.NULL_ASSERT(_selectPlayer);

            // 移動開始前の情報を保存
            _selectPlayer.AdaptPrevMoveInfo();
            var param = _selectPlayer.param;

            // キャラクターの現在の位置情報を保持
            StageController.Footprint footprint = new StageController.Footprint();
            footprint.gridIndex = _selectPlayer.GetCurrentGridIndex();
            footprint.rotation  = _selectPlayer.transform.rotation;
            _stageCtrl.LeaveFootprint(footprint);
            _stageCtrl.BindGridCursorState( GridCursor.State.MOVE, _selectPlayer);

            // 移動可能情報を登録及び表示
            bool isAttackable = !_selectPlayer.IsEndCommand( Character.Command.COMMAND_TAG.ATTACK );
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
                    var plGridIndex     = _selectPlayer.GetCurrentGridIndex();
                    stageGrid.FetchCurrentGridInfo(out info);

                    // 移動更新
                    _selectPlayer.UpdateMove(curGridIndex, info);
                    break;
                case PlMovePhase.PL_MOVE_END:
                    // 移動したキャラクターの移動コマンドを選択不可にする
                    _selectPlayer.SetEndCommandStatus(Character.Command.COMMAND_TAG.MOVE, true);
                    // コマンド選択に戻る
                    Back();

                    return true;
            }

            return false;
        }

        override public void Exit()
        {
            // 操作対象データをリセット
            _stageCtrl.ClearGridCursroBind();
            // 選択グリッドを表示
            _stageCtrl.SetGridCursorActive(true);
            // ステージグリッド上のキャラ情報を更新
            _stageCtrl.UpdateGridInfo();
            // グリッド状態の描画をクリア
            _stageCtrl.ClearGridMeshDraw();

            base.Exit();
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isRevert">キャンセル入力の有無</param>
        override protected bool AcceptRevertInput(bool isRevert)
        {
            if( base.AcceptRevertInput(isRevert) )
            {
                // 巻き戻しを行う
                Rewind();

                return true;
            }

            return false;
        }

        /// <summary>
        /// 決定入力受付の可否を判定します
        /// </summary>
        /// <returns>決定入力受付の可否</returns>
        private bool CanAcceptInputConfirm()
        {
            if (!CanAcceptInputDefault()) return false;

            // 攻撃対象選択フェーズでない場合は終了
            if (PlMovePhase.PL_MOVE == _phase) return true;

            return false;
        }

        /// <summary>
        /// 方向入力受付の可否を判定します
        /// </summary>
        /// <returns>方向入力受付の可否</returns>
        private bool CanAcceptInputDirection()
        {
            if (!CanAcceptInputDefault()) return false;

            // 移動フェーズでない場合、または移動入力受付が不可能である場合は終了
            if (PlMovePhase.PL_MOVE == _phase && _selectPlayer.IsAcceptableMovementOperation(_stageCtrl.GetGridSize())) return true;

            return false;
        }

        /// <summary>
        /// 決定入力を検知して状況毎に異なる処理を行います
        /// </summary>
        public bool AcceptConfirmInput( bool isConfirm )
        {
            if( !isConfirm ) { return false; }

            GridInfo info;
            var curGridIndex    = _stageCtrl.GetCurrentGridIndex();
            var plGridIndex     = _selectPlayer.GetCurrentGridIndex();
            _stageCtrl.FetchCurrentGridInfo(out info);

            // 出発地点と同一グリッドであれば戻る
            if (curGridIndex == _departGridIndex)
            {
                Back();
            }
            // 移動不可地点
            else if (info.estimatedMoveRange < 0)
            {
                // TODO : 移動不可であることを示すために効果音を鳴らすと良い
            }
            // キャラクターが存在していないことを確認
            else if (0 == (info.flag & (StageController.BitFlag.PLAYER_EXIST | StageController.BitFlag.ENEMY_EXIST | StageController.BitFlag.OTHER_EXIST)))
            {
                _phase = PlMovePhase.PL_MOVE_END;
            }

            return true;
        }
    }
}