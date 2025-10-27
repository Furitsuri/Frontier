using Frontier.Entities;
using Frontier.Stage;
using Zenject;

namespace Frontier.StateMachine
{
    /// <summary>
    /// 移動ステート中に直接攻撃へと遷移した際の攻撃選択ステートです
    /// </summary>
    public class PlAttackOnMoveState : PlAttackState
    {
        override public void Init()
        {
            PlPhaseStateInit(); // base.Init()は呼ばない(PlAttackState.Init()が呼ばれてしまうため)

            Character targetChara = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            NullCheck.AssertNotNull( targetChara, nameof( targetChara ) );

            _stageCtrl.ClearGridCursroBind();                          // 念のためバインドを解除
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _plOwner );     // グリッドカーソル位置を元に戻す

            _playerSkillNames   = _plOwner.Params.CharacterParam.GetEquipSkillNames();
            _attackSequence     = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>(false);
            _phase              = PlAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex    = _plOwner.Params.TmpParam.gridIndex;
            _targetCharacter    = null;

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _attackCharacter = _plOwner;
            var param = _attackCharacter.Params.CharacterParam;

            _plOwner.ActionRangeCtrl.SetupAttackableRangeData( _curentGridIndex );
            _attackCharacter.ActionRangeCtrl.DrawAttackableRange();

            // グリッドカーソル上のキャラクターを攻撃対象に設定
            if( _stageCtrl.TileDataHdlr().CorrectAttackableTileIndexs( _attackCharacter, targetChara ) )
            {
                _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _attackCharacter );  // アタッカーキャラクターの設定
                _uiSystem.BattleUi.ToggleAttackCursorP2E( true ); // アタックカーソルUI表示
            }

            _attackSequence.Init(); // 攻撃シーケンスを初期化
        }

        override public void ExitState()
        {
            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = _attackSequence.GetDiedCharacter();
            if ( diedCharacter != null )
            {
                var key = new CharacterKey(diedCharacter.Params.CharacterParam.characterTag, diedCharacter.Params.CharacterParam.characterIndex);
                NorifyCharacterDied( key );
                // 破棄
                diedCharacter.Dispose();
            }

            // アタッカーキャラクターの設定を解除
            _stageCtrl.ClearGridCursroBind();

            // 予測ダメージをリセット
            _attackCharacter.Params.TmpParam.SetExpectedHpChange( 0, 0 );
            _targetCharacter.Params.TmpParam.SetExpectedHpChange( 0, 0 );

            // アタックカーソルUI非表示
            _uiSystem.BattleUi.ToggleAttackCursorP2E( false );

            // ダメージ予測表示UIを非表示
            _uiSystem.BattleUi.ToggleBattleExpect( false );

            // 使用スキルの点滅を非表示
            for ( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                _uiSystem.BattleUi.GetPlayerParamSkillBox( i ).SetFlickEnabled( false );
                _uiSystem.BattleUi.GetPlayerParamSkillBox( i ).SetUseable( true );
                _uiSystem.BattleUi.GetEnemyParamSkillBox( i ).SetFlickEnabled( false );
                _uiSystem.BattleUi.GetEnemyParamSkillBox( i ).SetUseable( true );
            }

            // 使用スキルコスト見積もりをリセット
            _attackCharacter.Params.CharacterParam.ResetConsumptionActionGauge();
            _attackCharacter.Params.SkillModifiedParam.Reset();
            _targetCharacter.Params.CharacterParam.ResetConsumptionActionGauge();
            _targetCharacter.Params.SkillModifiedParam.Reset();

            _stageCtrl.ClearTileMeshDraw();                     // グリッドの描画をクリア
            _stageCtrl.SetGridCursorControllerActive( true );   // 選択グリッドを表示

            base.ExitState();
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        override protected void AdaptSelectPlayer()
        {
            _plOwner = _stageCtrl.GetBindCharacterFromGridCursor() as Player;
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }
    }
}