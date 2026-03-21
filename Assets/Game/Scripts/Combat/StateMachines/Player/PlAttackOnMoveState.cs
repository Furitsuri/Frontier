using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;

namespace Frontier.Battle
{
    /// <summary>
    /// 移動ステート中に直接攻撃へと遷移した際の攻撃選択ステートです
    /// </summary>
    public class PlAttackOnMoveState : PlAttackState
    {
        public override void Init()
        {
            PlPhaseStateInit(); // base.Init()は呼ばない(PlAttackState.Init()が呼ばれてしまうため)

            Character targetChara = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            NullCheck.AssertNotNull( targetChara, nameof( targetChara ) );

            _stageCtrl.ClearGridCursroBind();                          // 念のためバインドを解除
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _plOwner );     // グリッドカーソル位置を元に戻す

            _playerSkillNames   = _plOwner.GetStatusRef.GetEquipSkillNames();
            _attackSequence     = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>(false);
            _phase              = PlAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex    = _plOwner.BattleParams.TmpParam.CurrentTileIndex;
            _targetCharacter    = null;

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _plOwner.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( _curentGridIndex );
            _plOwner.BattleLogic.ActionRangeCtrl.DrawAttackableRange();

            // グリッドカーソル上のキャラクターを攻撃対象に設定
            if( _stageCtrl.TileDataHdlr().CorrectAttackableTileIndexs( _plOwner.BattleLogic.ActionRangeCtrl, targetChara ) )
            {
                _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _plOwner );    // アタッカーキャラクターの設定
                _uiSystem.BattleUi.SetAttackCursorP2EActive( true );                // アタックカーソルUI表示
            }

            _attackSequence.Init(); // 攻撃シーケンスを初期化

            // 使用可能スキルを更新
            _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );

            // パラメータビューにキャラクターを割り当て
            var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Left );
            _presenter.CharaParamView( ParameterWindowType.Left ).AssignCharacter( _plOwner, layerMaskIndex );
        }

        public override void ExitState()
        {
            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = _attackSequence.GetDiedCharacter();
            if ( diedCharacter != null )
            {
                var key = new CharacterKey(diedCharacter.GetStatusRef.characterTag, diedCharacter.GetStatusRef.characterIndex);
                NorifyCharacterDied( key );
                // 破棄
                diedCharacter.Dispose();
            }

            // アタッカーキャラクターの設定を解除
            _stageCtrl.ClearGridCursroBind();

            // 予測ダメージをリセット
            _plOwner.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            _targetCharacter.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );

            // アタックカーソルUI非表示
            _uiSystem.BattleUi.SetAttackCursorP2EActive( false );

            // ダメージ予測表示UIを非表示
            _uiSystem.BattleUi.ToggleBattleExpect( false );

            // 使用スキルコスト見積もりをリセット
            _plOwner.GetStatusRef.ResetConsumptionActionGauge();
            _plOwner.BattleParams.SkillModifiedParam.Reset();
            _plOwner.RefreshUseableSkillFlags( SituationType.NONE, 0xff );
            _targetCharacter.GetStatusRef.ResetConsumptionActionGauge();
            _targetCharacter.BattleParams.SkillModifiedParam.Reset();
            _targetCharacter.RefreshUseableSkillFlags( SituationType.NONE, 0xff );

            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();       // タイルメッシュの描画をすべてクリア
            _stageCtrl.SetGridCursorControllerActive( true );   // 選択グリッドを表示

            base.ExitState();
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