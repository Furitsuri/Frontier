using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;

namespace Frontier.Battle
{
    /// <summary>
    /// 連携候補となる予約済みアクションの攻撃範囲点滅を管理します。
    /// 候補の検索自体は CooperativeCandidateFinder に委譲します。
    /// </summary>
    public class CooperativeBlinkController
    {
        private readonly CooperativeCandidateFinder _candidateFinder;
        private List<Character> _blinkingAttackers = new List<Character>();

        public CooperativeBlinkController( SkillActionReservationQueue reservationQueue, BattleRoutineController btlRtnCtrl )
        {
            _candidateFinder = new CooperativeCandidateFinder( reservationQueue, btlRtnCtrl );
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

            var newAttackers = _candidateFinder.GetCooperativeAttackers( attackTargetCharaKeys );

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
    }
}
