using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.StateMachine;
using Frontier.UI;
using System;

namespace Frontier.Battle
{
    /// <summary>
    /// 移動ステート中に直接攻撃へと遷移した際の攻撃選択ステートです
    /// </summary>
    public class PlAttackOnMoveState : PlAttackState
    {
        public override void Init( object context )
        {
            PlPhaseStateInit(); // base.Init()は呼ばない(PlAttackState.Init()が呼ばれてしまうため)

            Character targetChara = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            NullCheck.AssertNotNull( targetChara, nameof( targetChara ) );

            _stageCtrl.UnbindGridCursor();                          // 念のためバインドを解除
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _plOwner );     // グリッドカーソル位置を元に戻す

            _playerSkillNames   = _plOwner.GetStatusRef.GetEquipSkillNames();
            _phase              = PlAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex    = _plOwner.BattleParams.TmpParam.CurrentTileIndex;
            _targetCharacter    = null;
            AccespuSubs         = new Func<InputContext, bool>[]
            {
                ( context ) => AcceptSub1Core( context ),
                ( context ) => AcceptSub2Core( context ),
                ( context ) => AcceptSub3Core( context ),
                ( context ) => AcceptSub4Core( context )
            };

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _plOwner.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( _curentGridIndex );
            _plOwner.BattleLogic.ActionRangeCtrl.DrawAttackableRange();

            // アタッカーキャラクターの設定
            _stageCtrl.BindGridCursor( GridCursorState.ATTACK, _plOwner );

            // グリッドカーソル上のキャラクターを攻撃対象に設定
            if( _stageCtrl.TryCollectAttackTargetTileIndicesWithFlag( _plOwner.BattleLogic.ActionRangeCtrl, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
            {
                _stageCtrl.MoveGridCursorToAttackableTile( targetChara );
                _uiSystem.BattleUi.SetActiveLeft2RightDirection( true );            // アタックカーソルUI表示
            }

            // 使用可能スキルを更新
            _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
            // 攻撃対象キャラクターの情報を更新
            RefreshTargetCharacter();
        }

        public override object ExitState()
        {
            OnExitStateAfterCombat(_plOwner, _targetCharacter );

            return base.ExitState();
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        protected override void AdaptSelectPlayer()
        {
            var selectCharacter = _stageCtrl.GetBindCharacterFromGridCursor();
            _plOwner            = _btlRtnCtrl.BtlCharaCdr.GetPlayer( selectCharacter.GetCharacterKey() );
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }
    }
}