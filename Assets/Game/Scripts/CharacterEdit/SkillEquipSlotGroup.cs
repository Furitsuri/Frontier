using System;
using System.Collections.Generic;
using System.Linq;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// 装備スキル設定画面における、1種類(アクティブ/パッシブ)の装備枠の「仮の割り振り」状態です。
    /// 仮の装備構成と、その種類のスキルの仮の所持数を保持します。
    /// アクティブスキルとパッシブスキルはIDの型が異なるため、型引数で種類を区別し、
    /// 割り振りの手続き(装備時に所持数を1消費、外す・入れ替え時に1戻す等)を共通化しています。
    /// 同一スキル(同一系統。強化段階が異なっても同じIDのもの)は、1人が複数の枠に装備することは出来ません。
    /// </summary>
    /// <typeparam name="TSkillID">スキルIDの型(ActiveSkillID/PassiveSkillID)</typeparam>
    public class SkillEquipSlotGroup<TSkillID> where TSkillID : struct, Enum
    {
        private static readonly EqualityComparer<TSkillID> IdComparer = EqualityComparer<TSkillID>.Default;

        private readonly TSkillID[] _tentativeSlots;
        private readonly Dictionary<TSkillID, int> _originalCounts;
        private readonly Dictionary<TSkillID, int> _tentativeCounts;
        private readonly Func<TSkillID, bool> _isValidSkill;
        private readonly TSkillID _none;

        /// <param name="equippedSlots">現在の装備構成(複製して保持するため、元の配列は書き換えません)</param>
        /// <param name="slotNum">枠数。equippedSlotsの要素数が一致しない場合は、不足分を未装備で補完します</param>
        /// <param name="inventory">現在の所持数(スキルIDと所持数の組)</param>
        /// <param name="isValidSkill">有効なスキルIDかどうかの判定</param>
        /// <param name="none">未装備を表す値</param>
        public SkillEquipSlotGroup( TSkillID[] equippedSlots, int slotNum, IEnumerable<KeyValuePair<TSkillID, int>> inventory,
                                    Func<TSkillID, bool> isValidSkill, TSkillID none )
        {
            _isValidSkill   = isValidSkill;
            _none           = none;

            _tentativeSlots = new TSkillID[slotNum];
            for ( int i = 0; i < slotNum; ++i )
            {
                _tentativeSlots[i] = ( equippedSlots != null && i < equippedSlots.Length ) ? equippedSlots[i] : none;
            }

            _originalCounts = new Dictionary<TSkillID, int>();
            foreach ( var entry in inventory )
            {
                _originalCounts[entry.Key] = entry.Value;
            }

            // 現在装備中のスキルが所持数リストに存在しない場合(初期装備等)も一覧に表示できるよう、
            // 0個所持として補完しておく
            foreach ( var skillID in _tentativeSlots )
            {
                if ( !_isValidSkill( skillID ) ) { continue; }
                if ( !_originalCounts.ContainsKey( skillID ) ) { _originalCounts[skillID] = 0; }
            }

            _tentativeCounts = new Dictionary<TSkillID, int>( _originalCounts );
        }

        public int SlotCount => _tentativeSlots.Length;

        /// <summary>
        /// 仮の装備構成です。
        /// </summary>
        public IReadOnlyList<TSkillID> Slots => _tentativeSlots;

        /// <summary>
        /// 指定枠に仮に装備されているスキルを返します。
        /// </summary>
        public TSkillID GetEquippedSkill( int slotIndex ) => _tentativeSlots[slotIndex];

        /// <summary>
        /// 指定スキルの仮の残り所持数を返します(所持したことのないスキルは0)。
        /// </summary>
        public int GetTentativeCount( TSkillID skillID )
        {
            return _tentativeCounts.TryGetValue( skillID, out var count ) ? count : 0;
        }

        /// <summary>
        /// 所持したことのある(所持数が0のものも含む)スキルIDをID順で返します。
        /// </summary>
        public IReadOnlyList<TSkillID> GetOwnedSkillIdsOrdered()
        {
            return _originalCounts.Keys.OrderBy( id => id ).ToList();
        }

        /// <summary>
        /// 指定スキルが、指定枠以外の枠に仮に装備されているかを返します。
        /// </summary>
        public bool IsEquippedInOtherSlot( TSkillID skillID, int exceptSlotIndex )
        {
            for ( int i = 0; i < _tentativeSlots.Length; ++i )
            {
                if ( i != exceptSlotIndex && IdComparer.Equals( _tentativeSlots[i], skillID ) ) { return true; }
            }

            return false;
        }

        /// <summary>
        /// 指定スキルを指定枠へ装備出来るかを返します。在庫が無い場合、既にその枠に装備されている場合、
        /// 及び他の枠に同一スキルを装備済みの場合は装備出来ません。
        /// </summary>
        public bool CanEquip( int slotIndex, TSkillID skillID )
        {
            if ( !_isValidSkill( skillID ) ) { return false; }
            if ( GetTentativeCount( skillID ) <= 0 ) { return false; }
            if ( IdComparer.Equals( _tentativeSlots[slotIndex], skillID ) ) { return false; }
            if ( IsEquippedInOtherSlot( skillID, slotIndex ) ) { return false; }

            return true;
        }

        /// <summary>
        /// 指定枠へ指定スキルを仮に装備します。既に別のスキルが入っていた場合はそのスキルの
        /// 所持数を1戻し、新しいスキルの所持数を1消費します。装備出来ない場合(CanEquip参照)は何もせずfalseを返します。
        /// </summary>
        public bool Equip( int slotIndex, TSkillID newSkillID )
        {
            if ( !CanEquip( slotIndex, newSkillID ) ) { return false; }

            var oldSkillID = _tentativeSlots[slotIndex];
            if ( _isValidSkill( oldSkillID ) )
            {
                _tentativeCounts[oldSkillID] = GetTentativeCount( oldSkillID ) + 1;
            }

            _tentativeCounts[newSkillID] = GetTentativeCount( newSkillID ) - 1;
            _tentativeSlots[slotIndex] = newSkillID;

            return true;
        }

        /// <summary>
        /// 指定枠の装備を仮に解除します。装備されていたスキルの所持数を1戻します。
        /// 元々未装備だった場合は何もせずfalseを返します。
        /// </summary>
        public bool Unequip( int slotIndex )
        {
            var oldSkillID = _tentativeSlots[slotIndex];
            if ( !_isValidSkill( oldSkillID ) ) { return false; }

            _tentativeCounts[oldSkillID] = GetTentativeCount( oldSkillID ) + 1;
            _tentativeSlots[slotIndex] = _none;

            return true;
        }

        /// <summary>
        /// 仮の装備構成を指定の配列へ書き込み、元の所持数からの増減分をapplyCountDeltaで通知します。
        /// </summary>
        public void CommitTo( TSkillID[] destSlots, Action<TSkillID, int> applyCountDelta )
        {
            for ( int i = 0; i < _tentativeSlots.Length && i < destSlots.Length; ++i )
            {
                destSlots[i] = _tentativeSlots[i];
            }

            foreach ( var kvp in _tentativeCounts )
            {
                int original = _originalCounts.TryGetValue( kvp.Key, out var value ) ? value : 0;
                int delta = kvp.Value - original;
                if ( delta != 0 )
                {
                    applyCountDelta( kvp.Key, delta );
                }
            }
        }
    }
}
