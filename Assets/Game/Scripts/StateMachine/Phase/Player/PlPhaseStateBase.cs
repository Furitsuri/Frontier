using Frontier;
using Frontier.Battle;
using Frontier.Stage;
using Frontier.Entities;
using UnityEngine;
using Zenject;

namespace Frontier.StateMachine
{
    public class PlPhaseStateBase : PhaseStateBase
    {
        protected Player _plOwner = null;

        virtual protected void AdaptSelectPlayer() { }

        /// <summary>
        /// 以前の状態に巻き戻します
        /// </summary>
        protected void Rewind()
        {
            if ( _plOwner == null ) { return; }

            _plOwner.RewindToPreviousState();
            _stageCtrl.TileDataHdlr().UpdateTileDynamicDatas();    // グリッド情報を更新
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _plOwner );
        }

        override public void Init()
        {
            base.Init();

            AdaptSelectPlayer();
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptCancel( bool isCancel )
        {
            if ( !isCancel ) return false;

            Back();

            return true;
        }
    }
}