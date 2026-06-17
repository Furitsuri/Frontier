using Frontier.Entities;
using Frontier.StateMachine;

namespace Frontier.Battle
{
    public class PlPhaseStateBase : UnitPhaseState
    {
        protected Player _plOwner = null;

        virtual protected void AdaptSelectPlayer() { }

        /// <summary>
        /// 以前の状態に巻き戻します
        /// </summary>
        protected void Rewind()
        {
            if ( _plOwner == null ) { return; }

            _plOwner.RevertBeforeMoving();
            _stageCtrl.SyncGridCursorAfterRevert( _plOwner );
        }

        /// <summary>
        /// コマンド履歴から直前のコマンドを取り消し、グリッドカーソルを元の位置へ同期します
        /// </summary>
        protected void RevertCommandHistory()
        {
            var commandTag = _plOwner.PopCommandHistory();
            _plOwner.BattleParams.TmpParam.SetEndCommandStatus( commandTag, false );
            _stageCtrl.SyncGridCursorAfterRevert( _plOwner );
        }

        public override void Init( object context )
        {
            base.Init( context );

            AdaptSelectPlayer();
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            Back();

            return true;
        }
    }
}