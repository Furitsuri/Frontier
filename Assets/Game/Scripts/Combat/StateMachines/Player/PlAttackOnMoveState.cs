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

            _stageCtrl.ClearGridCursorBind();                          // 念のためバインドを解除
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

            // グリッドカーソル上のキャラクターを攻撃対象に設定
            if( _stageCtrl.TileDataHdlr().CollectAttackableTileIndicesWithFlag( _plOwner.BattleLogic.ActionRangeCtrl.ActionableTileMap.AttackableTileMap, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
            {
                _stageCtrl.MoveGridCursorToAttackableTile( targetChara );
                _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _plOwner );    // アタッカーキャラクターの設定
                _uiSystem.BattleUi.SetActiveLeft2RightDirection( true );            // アタックカーソルUI表示
            }

            // 使用可能スキルを更新
            _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
        }

        public override object ExitState()
        {
            _stageCtrl.ClearGridCursorBind();                       // アタッカーキャラクターの設定を解除
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _plOwner );

            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = null; // _attackSequence.GetDiedCharacter();
            if ( diedCharacter != null )
            {
                var key = new CharacterKey(diedCharacter.GetStatusRef.characterTag, diedCharacter.GetStatusRef.characterIndex);
                NorifyCharacterDied( key );
                // 破棄
                diedCharacter.Dispose();
            }

			_presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );    // アクション対象指定関連のUIを非表示

			// 予測ダメージをリセット
			_plOwner.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            _targetCharacter.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );

            // 使用スキルコスト見積もりをリセット
            _plOwner.RefreshUseableSkillFlags( SituationType.NONE, 0xff );
            _targetCharacter.RefreshUseableSkillFlags( SituationType.NONE, 0xff );

            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();       // タイルメッシュの描画をすべてクリア
            _stageCtrl.SetActiveGridCursor( true );   // 選択グリッドを表示

            return base.ExitState();
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        protected override void AdaptSelectPlayer()
        {
            _plOwner = _stageCtrl.GetBindCharacterFromGridCursor() as Player;
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }
    }
}