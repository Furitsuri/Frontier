using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Frontier.Battle
{
    /// <summary>
    /// 連携候補となる予約済みアクションの攻撃範囲点滅を管理します。
    /// SkillActionReservationQueue を参照し、現在の攻撃対象と重複する予約アクションを検索します。
    /// </summary>
    public class CooperativeBlinkController
    {
        private readonly SkillActionReservationQueue _reservationQueue;
        private readonly BattleRoutineController _btlRtnCtrl;
        private List<Character> _blinkingAttackers = new List<Character>();

        public CooperativeBlinkController( SkillActionReservationQueue reservationQueue, BattleRoutineController btlRtnCtrl )
        {
            _reservationQueue = reservationQueue;
            _btlRtnCtrl       = btlRtnCtrl;
        }

        /// <summary>
        /// 現在の攻撃対象リストをもとに点滅状態を更新します。
        /// IsCooperative でないスキルの場合は点滅を全停止します。
        /// </summary>
        public void Refresh( List<CharacterKey> attackTargetCharaKeys, SkillID useSkillID )
        {
            if( !SkillsData.data[( int ) useSkillID].IsCooperative )
            {
                StopAll();
                return;
            }

            var newAttackers = FindCooperativeAttackers( attackTargetCharaKeys );

            foreach( var attacker in _blinkingAttackers )
            {
                if( !newAttackers.Contains( attacker ) )
                {
                    attacker.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.SetBlinkTargetableRange( false );
                }
            }
            foreach( var attacker in newAttackers )
            {
                if( !_blinkingAttackers.Contains( attacker ) )
                {
                    attacker.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.SetBlinkTargetableRange( true );
                }
            }

            _blinkingAttackers = newAttackers;
        }

        /// <summary>
        /// 全ての点滅を停止し、点滅リストをクリアします。
        /// </summary>
        public void StopAll()
        {
            foreach( var attacker in _blinkingAttackers )
            {
                attacker.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.SetBlinkTargetableRange( false );
            }
            _blinkingAttackers.Clear();
        }

        /// <summary>
        /// 指定の攻撃対象リストと重複する予約済みアクションの攻撃者リストを返します。
        /// AcceptConfirm などで選択肢の構築に使用します。
        /// </summary>
        public List<Character> GetCooperativeAttackers( List<CharacterKey> attackTargetCharaKeys )
        {
            return FindCooperativeAttackers( attackTargetCharaKeys );
        }

        /// <summary>
        /// 指定の攻撃対象リストと重複する予約済みアクションのデータ一覧を返します。
        /// 連携攻撃確定前の合計ダメージ集計などに使用します。
        /// </summary>
        public List<SkillActionReservationData> GetCooperativeReservations( List<CharacterKey> attackTargetCharaKeys )
        {
            return FindCooperativeReservations( attackTargetCharaKeys );
        }

        private List<Character> FindCooperativeAttackers( List<CharacterKey> attackTargetCharaKeys )
        {
            var result = new List<Character>();
            foreach( var reservation in FindCooperativeReservations( attackTargetCharaKeys ) )
            {
                var attacker = _btlRtnCtrl.BtlCharaCdr.GetCharacter( reservation.AttackerKey );
                if( attacker != null && !result.Contains( attacker ) )
                {
                    result.Add( attacker );
                }
            }
            return result;
        }

        private List<SkillActionReservationData> FindCooperativeReservations( List<CharacterKey> attackTargetCharaKeys )
        {
            var result = new List<SkillActionReservationData>();
            foreach( var reservation in _reservationQueue.GetAll() )
            {
                bool hasCommonTarget = reservation.AttackTargetCharaKeys.Any( key => attackTargetCharaKeys.Contains( key ) );
                if( hasCommonTarget ) { result.Add( reservation ); }
            }
            return result;
        }
    }
}
