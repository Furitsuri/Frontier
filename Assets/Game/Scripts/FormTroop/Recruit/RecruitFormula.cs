using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.BattleFileLoader;
using static Constants;

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
        static public ( int unitTypeIndex, int cost, CharacterStatusData statusData ) GenerateEmploymentCandidateData( int level, int characterIndex, UnitLevelStatsContainer unitLevelStats, List<CharacterCandidate> candidatedMercenaries )
        {
            int unitTypeIndex       = SelectBalancedTypeIndex( candidatedMercenaries );
            int levelIndex          = Mathf.Clamp( level, 0, 10 );

            // Random.Rangeは第一引数はInclusiveですが第二引数がExclusiveであることに注意してください
            int hpRandValue         = Random.Range( -3, 4 );
            int atkRandValue        = Random.Range( -2, 3 );
            int defRandValue        = Random.Range( -2, 3 );
            int actMaxRandValue     = Random.Range( 0, 2 );
            int actRecovRandValue   = Random.Range( 0, 2 );

            CharacterStatusData statusData = unitLevelStats.Stats[unitTypeIndex].StatusDatas[levelIndex];

            // TODO : 仮実装
            string[] names = new string[] { "Joseph", "Micky", "Mark", "Tarner" };
            statusData.Name = names[Random.Range( 0, 4 )];

            statusData.CharacterIndex = characterIndex;

            statusData.MaxHP += hpRandValue;
            statusData.Atk += atkRandValue;
            statusData.Def += defRandValue;
            statusData.ActGaugeMax += actMaxRandValue;
            statusData.ActRecovery += actRecovRandValue;

            int employmentCost = CalcurateEmploymentCost( unitTypeIndex, statusData.Level, hpRandValue, atkRandValue, defRandValue, actMaxRandValue, actRecovRandValue );

            return ( unitTypeIndex, employmentCost, statusData );
        }

        /// <summary>
        /// キャラクターの雇用コストを、
        /// ユニットタイプとレベル、及び、
        /// 各パラメータに加算される乱数値(HP, 攻撃力, 防御力, アクションゲージ最大値及び回復値)
        /// から計算します
        /// </summary>
        /// <param name="unitType"></param>
        /// <param name="level"></param>
        /// <param name="hp"></param>
        /// <param name="atk"></param>
        /// <param name="def"></param>
        /// <param name="actMax"></param>
        /// <param name="actRecov"></param>
        /// <returns></returns>
        static private int CalcurateEmploymentCost( int unitType, int level, int hp, int atk, int def, int actMax, int actRecov )
        {
            // TODO : 仮で5としているが、ユニットタイプの総数が決まり次第、定数に変更し、尚且つファイル読込からコストを取得すること
            int[] unitCost = new int[5] { 10, 13, 15, 18, 20 }; 

            return
                unitCost[unitType] + 
                level * COEFFICIENT_RECRUIT_COST_LV + 
                hp * COEFFICIENT_RECRUIT_COST_HP + 
                atk * COEFFICIENT_RECRUIT_COST_ATK + 
                def * COEFFICIENT_RECRUIT_COST_DEF + 
                actMax * COEFFICIENT_RECRUIT_COST_ACT_MAX + 
                actRecov * COEFFICIENT_RECRUIT_COST_ACT_RECOV;
        }

        static private int SelectBalancedTypeIndex( List<CharacterCandidate> candidatedMercenaries )
        {
            return 0;
        }
    }
}
