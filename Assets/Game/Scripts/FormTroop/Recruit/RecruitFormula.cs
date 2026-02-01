using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.BattleFileLoader;

namespace Frontier.FormTroop
{
    public static class RecruitFormula
    {
        /// <summary>
        /// ステージレベルと既存の雇用候補キャラクターリストを基にユニットタイプ、雇用コスト、ステータスデータを生成します
        /// </summary>
        /// <param name="stageLevel"></param>
        /// <param name="candidatedMercenaries"></param>
        /// <returns></returns>
        static public ( int unitTypeIndex, int cost, CharacterStatusData statusData ) GenerateEmploymentCandidateData( int stageLevel, int characterIndex, List<CharacterCandidate> candidatedMercenaries )
        {
            int unitTypeIndex               = Random.Range( 0, 2 );
            int employmentCost              = Random.Range( 1, 11);
            CharacterStatusData statusData  = new CharacterStatusData()
            {
                Name = "Hero",
                CharacterTag = ( int ) CHARACTER_TAG.PLAYER,
                CharacterIndex = characterIndex,
                MaxHP = 25 + Random.Range( 0, 6 ),
                Atk = 20,
                Def = 10,
                MoveRange = 5,
                JumpForce = 2,
                AtkRange = 1,
                ActGaugeMax = 5,
                ActRecovery = 2,
                InitGridIndex = 0,
                InitDir = ( int ) Direction.FORWARD,
                Skills = new int[] { -1, -1, -1, -1 },
            };

            return ( unitTypeIndex, employmentCost, statusData );
        }
    }
}
