using Frontier.Entities;
using System.Collections.Generic;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// NPCが行動(通常攻撃・スキルによる攻撃・回復等)を実行する直前の流れを共通化した、行動ステートの基底クラスです。
    ///
    /// 実行前の流れ:
    ///   ・プレイヤーが介入する手立てがある場合(攻撃対象のプレイヤーが防御スキルを使用出来る等)は、介入の操作(防御スキルの選択等)へ移り、
    ///     その操作を終えた時点で実行します。
    ///   ・介入する手立てが無い場合は、確認の入力を待たず、行動の内容(パラメータウィンドウ・範囲等)を一定時間(NPC_ACTION_AUTO_EXECUTE_DISPLAY_TIME)
    ///     表示し、かつ向き直り等の準備が整った時点で実行します。
    /// 派生クラスは、Init内で行動の表示を終えた後にBeginActionConfirmを呼び、Update内でUpdateActionConfirmの結果に従って介入の操作または実行を行います。
    /// </summary>
    public abstract class NpcActionStateBase : UnitPhaseState
    {
        /// <summary>
        /// 実行前の確認の進行結果です
        /// </summary>
        protected enum ActionConfirmResult
        {
            WAITING,                // 表示中・準備中のため待機する
            REQUEST_INTERVENTION,   // プレイヤーの介入の操作へ移る
            EXECUTE,                // 行動を実行する
        }

        private bool _canPlayerIntervene    = false;
        private float _elapsedTime          = 0f;   // 行動の内容の表示を開始してからの経過時間

        /// <summary>
        /// NPCの行動に介入する手立てがあるプレイヤーを求めます。
        /// 現状、介入の手立ては「攻撃を受けるプレイヤーが防御スキルを使用する」のみです。
        /// 事前にEmDefenceSkillSelectState.RefreshDefenceUseableSkillFlagsで、攻撃の種類に応じた防御スキルの使用可否を更新しておく必要があります
        /// </summary>
        /// <param name="targets">行動の対象</param>
        /// <returns>介入の手立てがあるプレイヤー(対象の並び順)</returns>
        static protected List<Player> CollectInterveningPlayers( IEnumerable<Character> targets )
        {
            var players = new List<Player>();
            foreach( var target in targets )
            {
                if( target is Player defender && !players.Contains( defender ) && EmDefenceSkillSelectState.HasUseableDefenceSkill( defender ) )
                {
                    players.Add( defender );
                }
            }

            return players;
        }

        /// <summary>
        /// 実行前の確認を開始します。行動の内容の表示を終えた時点で呼び出してください
        /// </summary>
        /// <param name="canPlayerIntervene">プレイヤーが介入する手立てがあるか(CollectInterveningPlayersで1人以上見つかったか)</param>
        protected void BeginActionConfirm( bool canPlayerIntervene )
        {
            _canPlayerIntervene = canPlayerIntervene;
            _elapsedTime        = 0f;
        }

        /// <summary>
        /// 実行前の確認を進めます
        /// </summary>
        /// <param name="isReady">実行の準備(向き直り等)が整っているか</param>
        /// <returns>確認の進行結果</returns>
        protected ActionConfirmResult UpdateActionConfirm( bool isReady )
        {
            _elapsedTime += DeltaTimeProvider.DeltaTime;

            if( !isReady ) { return ActionConfirmResult.WAITING; }

            // 介入の手立てがある場合は、表示の時間を待たずに介入の操作へ移る(操作の間も行動の内容は表示されたままのため)
            if( _canPlayerIntervene ) { return ActionConfirmResult.REQUEST_INTERVENTION; }

            return ( NPC_ACTION_AUTO_EXECUTE_DISPLAY_TIME <= _elapsedTime ) ? ActionConfirmResult.EXECUTE : ActionConfirmResult.WAITING;
        }
    }
}
