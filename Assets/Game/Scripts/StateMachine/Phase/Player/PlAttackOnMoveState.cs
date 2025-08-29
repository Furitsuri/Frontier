using Frontier.Entities;
using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// 移動ステート中に直接攻撃へと遷移した際の攻撃選択ステートです
    /// </summary>
    public class PlAttackOnMoveState : PlAttackState
    {
        override public void Init()
        {
            Character targetChara = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();

            PlPhaseStateInit();

            // PlMoveStateでBindを解除せずに遷移しているので、ここで取得
            _selectPlayer = _stageCtrl.GetGridCursorControllerBindCharacter() as Player;
            _stageCtrl.ClearGridCursroBind();                           // 念のため解除
            _stageCtrl.ApplyCurrentGrid2CharacterGrid(_selectPlayer);   // グリッドカーソル位置を元に戻す

            _playerSkillNames   = _selectPlayer.Params.CharacterParam.GetEquipSkillNames();
            _attackSequence     = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>(false);
            _phase              = PlAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex    = _selectPlayer.Params.TmpParam.gridIndex;
            _targetCharacter    = null;

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _attackCharacter = _selectPlayer;
            var param = _attackCharacter.Params.CharacterParam;
            _stageCtrl.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
            _stageCtrl.DrawAttackableGrids(_curentGridIndex);

            // グリッドカーソル上のキャラクターを攻撃対象に設定
            if (_stageCtrl.RegistAttackTargetGridIndexs(CHARACTER_TAG.ENEMY, targetChara))
            {
                _stageCtrl.BindGridCursorControllerState(GridCursorController.State.ATTACK, _attackCharacter);  // アタッカーキャラクターの設定
                _uiSystem.BattleUi.ToggleAttackCursorP2E(true); // アタックカーソルUI表示
            }

            // 攻撃シーケンスを初期化
            _attackSequence.Init();
        }
    }
}