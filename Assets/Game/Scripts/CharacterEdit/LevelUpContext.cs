using Frontier.Entities;
using static Constants;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// レベルアップ画面における「仮の割り振り」状態を保持します。
    /// 実際のCharacter.Status/UserDomain.Expはここでは一切書き換えず、
    /// 決定(OK)操作が行われた際にLevelUpHandlerが本データへ反映します。
    /// エルデンリング/ダークソウルのレベルアップ画面と同様、能力値を1ポイント上げるごとに
    /// レベルが1上がり、そのレベル到達に必要な経験値(部隊共有ポイント)が消費されます。
    /// </summary>
    public class LevelUpContext
    {
        public enum StatKind
        {
            MaxHP = 0,
            Atk,
            Def,

            NUM,
        }

        public Character Character { get; }
        public int OriginalLevel { get; }
        public int OriginalExp { get; }

        public int TentativeLevel { get; private set; }
        public int TentativeExp { get; private set; }

        private readonly int[] _allocated = new int[( int ) StatKind.NUM];

        /// <param name="character">レベルアップ対象のキャラクター</param>
        /// <param name="currentExp">部隊共有ポイントの現在値(UserDomain.Exp)</param>
        public LevelUpContext( Character character, int currentExp )
        {
            Character = character;

            var status = character.GetStatusRef;
            OriginalLevel = status.Level;
            OriginalExp   = currentExp;

            TentativeLevel = OriginalLevel;
            TentativeExp   = currentExp;
        }

        public int GetAllocated( StatKind stat ) => _allocated[( int ) stat];

        /// <summary>
        /// 割り振り分を反映した、指定能力値の仮の値を返します。
        /// </summary>
        public int GetTentativeStatValue( StatKind stat )
        {
            var status = Character.GetStatusRef;
            int baseValue = GetBaseStatValue( status, stat );

            return baseValue + _allocated[( int ) stat] * GetGrowthPerPoint( stat );
        }

        public static int GetBaseStatValue( in Status status, StatKind stat )
        {
            switch ( stat )
            {
                case StatKind.MaxHP: return status.MaxHP;
                case StatKind.Atk:   return status.Atk;
                case StatKind.Def:   return status.Def;
                default:             return 0;
            }
        }

        public static int GetGrowthPerPoint( StatKind stat )
        {
            switch ( stat )
            {
                case StatKind.MaxHP: return LEVEL_UP_GROWTH_MAX_HP;
                case StatKind.Atk:   return LEVEL_UP_GROWTH_ATK;
                case StatKind.Def:   return LEVEL_UP_GROWTH_DEF;
                default:             return 0;
            }
        }

        /// <summary>
        /// 現在の仮レベルから次のレベルへ上げるために必要なポイント数を返します。
        /// 最大レベルの場合は0を返します。
        /// </summary>
        public int GetNextLevelCost()
        {
            if ( LevelExpData.IsMaxLevel( TentativeLevel ) ) return 0;

            return LevelExpData.data[TentativeLevel + 1].RequiredTotalExp - LevelExpData.data[TentativeLevel].RequiredTotalExp;
        }

        public bool CanIncrease()
        {
            if ( LevelExpData.IsMaxLevel( TentativeLevel ) ) return false;

            return TentativeExp >= GetNextLevelCost();
        }

        public bool CanDecrease( StatKind stat )
        {
            return 0 < _allocated[( int ) stat];
        }

        /// <summary>
        /// 指定能力値に1ポイント割り振ります(レベルが1上がり、必要ポイント分が消費されます)。
        /// 割り振れなかった場合はfalseを返します。
        /// </summary>
        public bool Increase( StatKind stat )
        {
            if ( !CanIncrease() ) return false;

            TentativeExp -= GetNextLevelCost();
            TentativeLevel++;
            _allocated[( int ) stat]++;

            return true;
        }

        /// <summary>
        /// 指定能力値への割り振りを1ポイント取り消します(レベルが1下がり、消費分が払い戻されます)。
        /// 取り消せなかった場合はfalseを返します。
        /// </summary>
        public bool Decrease( StatKind stat )
        {
            if ( !CanDecrease( stat ) ) return false;

            // このレベルに上げた際に消費したポイントを払い戻す
            int refund = LevelExpData.data[TentativeLevel].RequiredTotalExp - LevelExpData.data[TentativeLevel - 1].RequiredTotalExp;

            TentativeExp += refund;
            TentativeLevel--;
            _allocated[( int ) stat]--;

            return true;
        }
    }
}
