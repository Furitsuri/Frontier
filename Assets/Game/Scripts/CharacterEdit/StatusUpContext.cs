using Frontier.Entities;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// ステータス上昇画面における「仮の割り振り」状態を保持します。
    /// 実際のCharacter.Statusはここでは一切書き換えず、決定(OK)操作が行われた際に
    /// StatusUpHandlerが本データへ反映します。
    /// LevelUpContextと違い、能力値毎に1ポイント上げるための必要StatusPointが異なり
    /// (StatusGrowthData参照)、レベルという概念も介さず直接そのStatusPointを消費します。
    /// また各能力値にはStatusGrowthData側で最小値・最大値が定義されており、範囲外への
    /// 割り振り/取り消しはできません。
    /// </summary>
    public class StatusUpContext
    {
        public enum StatKind
        {
            MaxHP = 0,
            Atk,
            Def,
            MoveRange,
            JumpForce,
            MaxActionGauge,
            RecoveryActionGauge,

            NUM,
        }

        public Character Character { get; }
        public int OriginalStatusPoint { get; }

        public int TentativeStatusPoint { get; private set; }

        private readonly int[] _allocated = new int[( int ) StatKind.NUM];

        /// <param name="character">ステータス上昇対象のキャラクター</param>
        public StatusUpContext( Character character )
        {
            Character = character;

            var status = character.GetStatusRef;
            OriginalStatusPoint = status.StatusPoint;
            TentativeStatusPoint = status.StatusPoint;
        }

        public int GetAllocated( StatKind stat ) => _allocated[( int ) stat];

        /// <summary>
        /// 割り振り分を反映した、指定能力値の仮の値を返します(1ポイント割り振る毎に能力値も1上がります)。
        /// </summary>
        public int GetTentativeStatValue( StatKind stat )
        {
            var status = Character.GetStatusRef;

            return GetBaseStatValue( status, stat ) + _allocated[( int ) stat];
        }

        public static int GetBaseStatValue( in Status status, StatKind stat )
        {
            switch ( stat )
            {
                case StatKind.MaxHP:               return status.MaxHP;
                case StatKind.Atk:                 return status.Atk;
                case StatKind.Def:                 return status.Def;
                case StatKind.MoveRange:           return status.moveRange;
                case StatKind.JumpForce:           return status.jumpForce;
                case StatKind.MaxActionGauge:       return status.maxActionGauge;
                case StatKind.RecoveryActionGauge: return status.recoveryActionGauge;
                default:                           return 0;
            }
        }

        private static StatusGrowthData.StatRange GetRange( StatKind stat )
        {
            var data = StatusGrowthData.data;
            switch ( stat )
            {
                case StatKind.MaxHP:               return data.MaxHP;
                case StatKind.Atk:                 return data.Atk;
                case StatKind.Def:                 return data.Def;
                case StatKind.MoveRange:           return data.moveRange;
                case StatKind.JumpForce:           return data.jumpForce;
                case StatKind.MaxActionGauge:       return data.maxActionGauge;
                case StatKind.RecoveryActionGauge: return data.recoveryActionGauge;
                default:                           return default;
            }
        }

        /// <summary>
        /// 指定能力値を1上げるために必要なStatusPointを返します(StatusGrowthData参照)。
        /// </summary>
        public static int GetCost( StatKind stat ) => GetRange( stat ).Cost;

        /// <summary>
        /// 指定能力値が取り得る最小値を返します(StatusGrowthData参照)。
        /// </summary>
        public static int GetMin( StatKind stat ) => GetRange( stat ).Min;

        /// <summary>
        /// 指定能力値が取り得る最大値を返します(StatusGrowthData参照)。
        /// </summary>
        public static int GetMax( StatKind stat ) => GetRange( stat ).Max;

        public bool CanIncrease( StatKind stat )
        {
            return TentativeStatusPoint >= GetCost( stat ) && GetTentativeStatValue( stat ) < GetMax( stat );
        }

        public bool CanDecrease( StatKind stat )
        {
            return 0 < _allocated[( int ) stat] && GetMin( stat ) < GetTentativeStatValue( stat );
        }

        /// <summary>
        /// 指定能力値に1ポイント割り振ります(必要StatusPointが消費されます)。
        /// 割り振れなかった場合はfalseを返します。
        /// </summary>
        public bool Increase( StatKind stat )
        {
            if ( !CanIncrease( stat ) ) return false;

            TentativeStatusPoint -= GetCost( stat );
            _allocated[( int ) stat]++;

            return true;
        }

        /// <summary>
        /// 指定能力値への割り振りを1ポイント取り消します(消費分が払い戻されます)。
        /// 取り消せなかった場合はfalseを返します。
        /// </summary>
        public bool Decrease( StatKind stat )
        {
            if ( !CanDecrease( stat ) ) return false;

            TentativeStatusPoint += GetCost( stat );
            _allocated[( int ) stat]--;

            return true;
        }
    }
}
