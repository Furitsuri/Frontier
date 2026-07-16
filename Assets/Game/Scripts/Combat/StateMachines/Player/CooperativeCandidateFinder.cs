using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Frontier.Battle
{
    /// <summary>
    /// 予約済みアクションの中から、指定した攻撃対象と重複する「連携候補」を検索します。
    /// SkillActionReservationQueue を参照します。
    /// </summary>
    public class CooperativeCandidateFinder
    {
        private readonly SkillActionReservationQueue _reservationQueue;
        private readonly BattleRoutineController _btlRtnCtrl;

        public CooperativeCandidateFinder( SkillActionReservationQueue reservationQueue, BattleRoutineController btlRtnCtrl )
        {
            _reservationQueue = reservationQueue;
            _btlRtnCtrl       = btlRtnCtrl;
        }

        /// <summary>
        /// 指定の攻撃対象リストと重複する予約済みアクションの攻撃者リストを返します。
        /// </summary>
        public List<Character> GetCooperativeAttackers( List<CharacterKey> attackTargetCharaKeys )
        {
            var result = new List<Character>();
            foreach( var reservation in GetCooperativeReservations( attackTargetCharaKeys ) )
            {
                var attacker = _btlRtnCtrl.BtlCharaCdr.GetCharacter( reservation.AttackerKey );
                if( attacker != null && !result.Contains( attacker ) )
                {
                    result.Add( attacker );
                }
            }
            return result;
        }

        /// <summary>
        /// 指定の攻撃対象リストと重複する予約済みアクションのデータ一覧を返します。
        /// </summary>
        public List<SkillActionReservationData> GetCooperativeReservations( List<CharacterKey> attackTargetCharaKeys )
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
