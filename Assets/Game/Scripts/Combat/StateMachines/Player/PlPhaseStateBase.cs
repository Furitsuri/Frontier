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
        /// 指定キャラクターのコマンド履歴から直前のコマンドを取り消し、グリッドカーソルを元の位置へ同期します
        /// </summary>
        protected void RevertCommandHistory( Player player )
        {
            if( COMMAND_TAG.NONE == player.RevertLastCommand() ) { return; }

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
        /// 後更新を行います。
        /// 行動の終了等で戻ろうとしている時点で、プレイヤーの全キャラクターが行動を終えている(最後のキャラクターが行動を終えた、
        /// またはターン終了が選ばれた)場合は、フェーズの終了を通知します。
        /// これにより、親ステート(コマンド選択・タイル選択等)へ1つずつ戻ることなく、このフレームのうちにフェーズが終了するため、
        /// 戻る度に行われる入力ガイドの登録・表示の更新等が挟まらず、次のフェーズへ滑らかに移行します。
        /// MEMO : 行動終了の判定(BattleLogicBase.UpdateActionEndState)はフェーズの更新の後に行われるため、Updateではなくここで判定します。
        ///        戦闘終了条件を満たしている場合は、従来通り勝敗判定・ステージクリア処理の開始を待つため通知しません。
        ///        予約中のスキルがある場合の実行確認は、フェーズ終了後にPlayerPhaseHandlerが挟みます。
        /// </summary>
        public override void LateUpdate()
        {
            base.LateUpdate();

            if( IsBack() && !_isEndedPhase && IsPlayerPhaseEnding() )
            {
                _isEndedPhase = true;
            }
        }

        /// <summary>
        /// プレイヤーの全キャラクターが行動を終え、プレイヤーフェーズを終了する状態であるかを取得します
        /// </summary>
        /// <returns>フェーズを終了する状態か</returns>
        protected bool IsPlayerPhaseEnding()
        {
            var btlCharaCdr = _btlRtnCtrl.BtlCharaCdr;

            return btlCharaCdr.IsEndAllArmyWaitCommand( CHARACTER_TAG.PLAYER ) && !btlCharaCdr.IsBattleEndConditionMet();
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