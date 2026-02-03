using Frontier.Entities;
using Frontier.StateMachine;
using Zenject;

namespace Frontier.Battle
{
    /// <summary>
    /// プレイヤーターン終了確認の選択画面
    /// </summary>
    public sealed class PlConfirmTurnEnd : ConfirmPhaseStateBase
    {
        [Inject] private BattleRoutineController _btlRtnCtrl = null;

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// /// <returns>決定入力の有無</returns>
        protected override bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) return false;

            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                // 全てのキャラクターを待機済みに設定して敵のフェーズに移行させる
                _btlRtnCtrl.BtlCharaCdr.ApplyAllArmyEndAction( CHARACTER_TAG.PLAYER );
            }

            Back();

            return true;
        }
    }
}