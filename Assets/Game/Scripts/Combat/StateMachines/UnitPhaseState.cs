using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using Frontier.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Battle
{
    public class UnitPhaseState : PhaseStateBase
    {
        [Inject] protected BattleRoutineController _btlRtnCtrl  = null;
        [Inject] protected StageController _stageCtrl           = null;

        protected BattleRoutinePresenter _presenter = null;

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as BattleRoutinePresenter;
        }

        /// <summary>
        /// 死亡したキャラクターの存在を通知します
        /// </summary>
        /// <param name="characterKey">死亡したキャラクターのハッシュキー</param>
        protected void NorifyCharacterDied( in CharacterKey characterKey )
        {
            _btlRtnCtrl.BtlCharaCdr.SetDiedCharacterKey( characterKey );
            _btlRtnCtrl.BtlCharaCdr.RemoveCharacterFromList( characterKey );
        }

        protected void OnExitStateAfterCombat( Character ownerChara, Character targetChara )
        {
            _stageCtrl.ClearGridCursorBind();                       // アタッカーキャラクターの設定を解除
            _stageCtrl.ApplyCurrentGrid2CharacterTile( ownerChara );

            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = null;
            if( ownerChara.GetStatusRef.IsDead() ) { diedCharacter = ownerChara; }
            if( targetChara != null && targetChara.GetStatusRef.IsDead() ) { diedCharacter = targetChara; }
            if( diedCharacter != null )
            {
                var key = new CharacterKey( diedCharacter.GetStatusRef.characterTag, diedCharacter.GetStatusRef.characterIndex );
                NorifyCharacterDied( key );
                diedCharacter.Dispose();    // 破棄
            }

            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );    // アクション対象指定関連のUIを非表示

            // 予測ダメージと使用スキルコスト見積もりをリセット
            if( null != ownerChara )
            {
                ownerChara.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            }
            if( null != targetChara )
            {
                targetChara.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            }

            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();   // タイルメッシュの描画をすべてクリア
            _stageCtrl.SetActiveGridCursor( true );         // 選択グリッドを表示
        }
    }
}