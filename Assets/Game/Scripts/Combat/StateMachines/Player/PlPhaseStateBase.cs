using Frontier.Combat;
using Frontier.Entities;
using Frontier.StateMachine;
using System.Collections.Generic;

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
        /// 指定キャラクターのコマンド履歴から直前のコマンドを取り消し、グリッドカーソルを元の位置へ同期します
        /// </summary>
        protected void RevertCommandHistory( Player player )
        {
            var commandTag = player.PopCommandHistory();
            if( COMMAND_TAG.NONE == commandTag ) { return; }

            player.BattleParams.TmpParam.SetEndCommandStatus( commandTag, false );
            _stageCtrl.SyncGridCursorAfterRevert( player );
        }

        /// <summary>
        /// 指定の移動先タイルのいずれかを「移動前の位置」としている、暫定移動中の他キャラクターの名前を集めます。
        /// それらのタイルへ移動すると、該当キャラクターは移動前の位置へ戻せなくなるため、確認ダイアログの表示判定に使用します。
        /// </summary>
        /// <param name="destinationTileIndices">これから移動する先のタイル</param>
        /// <param name="movers">これから移動するキャラクター(判定対象から除外する)</param>
        /// <returns>移動前の位置へ戻せなくなるキャラクター名(該当なしの場合は空)</returns>
        protected List<string> CollectUndoBlockedCharacterNames( ICollection<int> destinationTileIndices, ICollection<Player> movers )
        {
            var names = new List<string>();

            foreach( Player player in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER ) )
            {
                if( movers.Contains( player ) || !player.IsProvisionallyMoved() ) { continue; }

                if( destinationTileIndices.Contains( player.PrevMoveInformaiton.tmpParam.CurrentTileIndex ) )
                {
                    names.Add( player.GetStatusRef.Name );
                }
            }

            return names;
        }

        /// <summary>
        /// 攻撃・スキルの実行確定時、自己バフスキルが使用されていれば登録し、使用可能スキルフラグを更新します。
        /// </summary>
        protected void RegisterSelfBuffIfNeeded()
        {
            if( _plOwner.BattleLogic.RegistSelfBuffSequences() )
            {
                _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
            }
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