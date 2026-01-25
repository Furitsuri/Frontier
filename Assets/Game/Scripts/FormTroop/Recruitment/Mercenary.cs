using System.Collections.Generic;
using Frontier.Entities;
using Zenject;

namespace Frontier.FormTroop
{
    public class Mercenary : Player
    {
        private Character _character    = null;
        private int _cost               = 0;
        private bool _employed          = false;

        public Character Unit => _character;
        public int Cost => _cost;
        public bool Employed => _employed;

        /// <summary>
        /// ステージレベルと、既にリストアップされている傭兵リストを基にして雇用候補となる傭兵を設定します
        /// </summary>
        /// <param name="stageLevel"></param>
        /// <param name="candidateMercenaries"></param>
        public void Setup( int stageLevel, List<Mercenary> candidatedMercenaries )
        {
            _employed = false;

            _character = RecruitmentFormula.CreateCandidateMercenary( _character, stageLevel, candidatedMercenaries );
            _cost = RecruitmentFormula.CalculateEmploymentCost( _character );
        }
    }
}