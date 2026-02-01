using Frontier.Entities;
using UnityEngine;

namespace Frontier.Battle
{
    public class PlWaitState : PlPhaseStateBase
    {
        public override void Init()
        {
            base.Init();

            // 選択しているプレイヤーの行動をすべて終了
            _plOwner.RefBattleParams.TmpParam.EndAction();

            // 更新せずに終了
            Back();
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        protected override void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            _plOwner = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }
    }
}