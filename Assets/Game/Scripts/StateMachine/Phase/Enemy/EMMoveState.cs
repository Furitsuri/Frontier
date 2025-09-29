using Frontier.Combat;
using Frontier.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier
{
    public class EmMoveState : PhaseStateBase
    {
        private enum EmMovePhase
        {
            EM_MOVE_WAIT = 0,
            EM_MOVE_EXECUTE,
            EM_MOVE_END,
        }

        private EmMovePhase _Phase      = EmMovePhase.EM_MOVE_WAIT;
        private float _moveWaitTimer    = 0f;
        private Enemy _emOwner;

        /// <summary>
        /// 初期化します
        /// MEMO : 移動する経路自体は既にEmSelectStateで決定済みの想定で設計されています。
        ///        EmMoveStateの初期化時点でどこに移動するか(引いては何を行うか)を決定していては遅いためです。
        /// </summary>
        override public void Init()
        {
            base.Init();

            _moveWaitTimer = 0f;    // 移動開始までの待機時間をリセット

            // 現在選択中のキャラクター情報を取得して移動範囲を表示
            _emOwner = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Enemy;
            Debug.Assert(_emOwner != null);
            var param           = _emOwner.Params.CharacterParam;
            _stageCtrl.DrawAllTileInformationMeshes();

            // 移動目標地点が、現在地点であった場合は即時終了
            if( _emOwner.GetAi().MovePathHandler.ProposedMovePath.Count <= 0 )
            {
                _Phase = EmMovePhase.EM_MOVE_END;
            }
            // 移動前処理
            else
            {
                _stageCtrl.SetGridCursorControllerActive( true ); // 選択グリッドを表示

                _Phase = EmMovePhase.EM_MOVE_WAIT;
            }
        }

        override public bool Update()
        {
            switch (_Phase)
            {
                case EmMovePhase.EM_MOVE_WAIT:
                    _moveWaitTimer += DeltaTimeProvider.DeltaTime;
                    if( ENEMY_SHOW_MOVE_RANGE_TIME <= _moveWaitTimer )
                    {
                        _stageCtrl.SetGridCursorControllerActive( false );  // 選択グリッドを一時非表示

                        _Phase = EmMovePhase.EM_MOVE_EXECUTE;
                    }
                    break;
                case EmMovePhase.EM_MOVE_EXECUTE:
                    if( _emOwner.UpdateMovePath() )
                    {
                        _Phase = EmMovePhase.EM_MOVE_END;
                    }
                    break;
                case EmMovePhase.EM_MOVE_END:
                    _emOwner.Params.TmpParam.SetEndCommandStatus(Command.COMMAND_TAG.MOVE, true);   // 移動したキャラクターの移動コマンドを選択不可にする
                    Back(); // コマンド選択に戻る

                    return true;
            }

            return false;
        }

        override public void ExitState()
        {
            _stageCtrl.ApplyCurrentGrid2CharacterGrid(_emOwner);    // 敵の位置に選択グリッドを合わせる
            _stageCtrl.SetGridCursorControllerActive(true);         // 選択グリッドを表示
            _stageCtrl.UpdateTileInfo();                            // ステージグリッド上のキャラ情報を更新
            _stageCtrl.ClearGridMeshDraw();                         // グリッド状態の描画をクリア

            base.ExitState();
        }
    }
}