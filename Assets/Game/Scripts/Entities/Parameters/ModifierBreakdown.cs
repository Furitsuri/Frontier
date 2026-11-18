using System.Collections.Generic;

namespace Frontier.Entities
{
    /// <summary>
    /// 能力値の補正値が、どの要因(パッシブスキル・使用中のスキル等)によってどれだけ加算されているかの内訳を求めます。
    /// ステータス画面のツールチップ等で、補正値の理由をユーザーに示すために使用します。
    /// </summary>
    static public class ModifierBreakdown
    {
        /// <summary>
        /// 補正の要因1件分です(要因の名前と、基礎値に対する倍率の加算値)。
        /// </summary>
        public readonly struct Source
        {
            public readonly string Name;
            public readonly float Magnification;

            public Source( string name, float magnification )
            {
                Name            = name;
                Magnification   = magnification;
            }
        }

        /// <summary>
        /// 内訳1件分です(要因の名前と、その要因による補正値)。
        /// </summary>
        public readonly struct Entry
        {
            public readonly string Name;
            public readonly int Value;

            public Entry( string name, int value )
            {
                Name    = name;
                Value   = value;
            }
        }

        /// <summary>
        /// 補正値の合計(total)を、各要因の倍率に応じて振り分けた内訳を求めます。
        /// MEMO : 補正値は倍率を合算してから整数化しているため(BattleParameters.CalculateModifiedValue)、要因ごとに個別に整数化した値の
        ///        合計は total と一致しないことがあります。内訳の合計が必ず total と一致するよう、各要因の値を切り捨てた上で、
        ///        過不足分を端数の大きい要因から順に1ずつ割り当てます(最大剰余方式)。
        /// </summary>
        /// <param name="baseValue">補正前の基礎値</param>
        /// <param name="sources">補正の要因</param>
        /// <param name="total">実際に適用されている補正値の合計</param>
        /// <param name="dest">内訳の格納先(要因と同じ並び順で追加します)</param>
        static public void Apportion( int baseValue, IReadOnlyList<Source> sources, int total, List<Entry> dest )
        {
            int count = sources.Count;
            if ( count <= 0 ) { return; }

            var values      = new int[count];
            var remainders  = new float[count];
            int sum         = 0;
            for ( int i = 0; i < count; ++i )
            {
                float raw       = baseValue * sources[i].Magnification;
                values[i]       = ( int ) raw;
                remainders[i]   = raw - values[i];
                sum            += values[i];
            }

            // 合計との差分を、差分の符号の向きに端数が最も大きい要因へ1ずつ割り当てる
            int diff = total - sum;
            while ( diff != 0 )
            {
                int sign = ( 0 < diff ) ? 1 : -1;
                int pick = 0;
                for ( int i = 1; i < count; ++i )
                {
                    if ( sign * remainders[pick] < sign * remainders[i] ) { pick = i; }
                }

                values[pick]        += sign;
                remainders[pick]    -= sign;
                diff                -= sign;
            }

            for ( int i = 0; i < count; ++i )
            {
                dest.Add( new Entry( sources[i].Name, values[i] ) );
            }
        }
    }
}
