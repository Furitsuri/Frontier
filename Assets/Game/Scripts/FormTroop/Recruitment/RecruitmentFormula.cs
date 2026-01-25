using System;
using System.Collections.Generic;
using Frontier.Entities;
using UnityEngine;

namespace Frontier.FormTroop
{
    public static class RecruitmentFormula
    {
        static public Character CreateCandidateMercenary( Character chara, int stageLevel, List<Mercenary> candidatedMercenaries )
        {
            chara.GetStatusRef.Level = Math.Clamp( stageLevel + UnityEngine.Random.Range( -1, 2 ), 1 , 99 );

            return chara;
        }

        static public int CalculateEmploymentCost( Character chara )
        {
            return 0;
        }
    }
}
