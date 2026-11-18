using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// 装備スキル設定画面における「仮の割り振り」状態を保持します。
    /// 実際のCharacter.Status.EquipSkills/PassiveSkills/UserDomain.SkillInventoryはここでは一切書き換えず、
    /// 決定(OK)操作が行われた際にSkillEquipHandlerが本データへ反映します
    /// (それまでは全て仮の値であり、キャンセルすれば破棄されます)。
    /// アクティブ枠・パッシブ枠は別々の配列で保持しますが、所持数はスキルの種類を問わず共通の在庫として扱います。
    /// </summary>
    public class SkillEquipContext
    {
        public Character Character { get; }

        private readonly UserDomain _userDomain;
        private readonly SkillID[] _tentativeActiveSkills;
        private readonly SkillID[] _tentativePassiveSkills;
        private readonly Dictionary<SkillID, int> _originalCounts;
        private readonly Dictionary<SkillID, int> _tentativeCounts;

        public SkillEquipContext( Character character, UserDomain userDomain )
        {
            Character = character;
            _userDomain = userDomain;

            var status = character.GetStatusRef;
            _tentativeActiveSkills  = CopySlots( status.EquipSkills, Constants.EQUIPABLE_ACTIVE_SKILL_MAX_NUM );
            _tentativePassiveSkills = CopySlots( status.PassiveSkills, Constants.EQUIPABLE_PASSIVE_SKILL_MAX_NUM );

            _originalCounts = new Dictionary<SkillID, int>();
            foreach ( var entry in userDomain.SkillInventory )
            {
                _originalCounts[entry.SkillID] = entry.Count;
            }

            // 現在装備中のスキルが所持数リストに存在しない場合(初期装備等)も一覧に表示できるよう、
            // 0個所持として補完しておく
            foreach ( var skillID in _tentativeActiveSkills.Concat( _tentativePassiveSkills ) )
            {
                if ( !SkillsData.IsValidSkill( skillID ) ) { continue; }
                if ( !_originalCounts.ContainsKey( skillID ) ) { _originalCounts[skillID] = 0; }
            }

            _tentativeCounts = new Dictionary<SkillID, int>( _originalCounts );
        }

        /// <summary>
        /// 指定種類の装備枠の数を返します。
        /// </summary>
        public int GetSlotCount( SkillSlotType slotType ) => GetSlots( slotType ).Length;

        /// <summary>
        /// 指定種類・指定枠に仮に装備されているスキルを返します。
        /// </summary>
        public SkillID GetEquippedSkill( SkillSlotType slotType, int slotIndex ) => GetSlots( slotType )[slotIndex];

        /// <summary>
        /// 指定スキルの仮の残り所持数を返します(所持したことのないスキルは0)。
        /// </summary>
        public int GetTentativeCount( SkillID skillID )
        {
            return _tentativeCounts.TryGetValue( skillID, out var count ) ? count : 0;
        }

        /// <summary>
        /// 所持したことのある(所持数が0のものも含む)スキルIDをID順で返します。
        /// </summary>
        public IReadOnlyList<SkillID> GetOwnedSkillIdsOrdered()
        {
            return _originalCounts.Keys.OrderBy( id => ( int ) id ).ToList();
        }

        /// <summary>
        /// 指定種類の枠に装備可能なスキルに絞り込んで、所持したことのあるスキルIDをID順で返します。
        /// </summary>
        public IReadOnlyList<SkillID> GetOwnedSkillIdsOrdered( SkillSlotType slotType )
        {
            return GetOwnedSkillIdsOrdered().Where( id => SkillsData.IsSkillMatchSlotType( id, slotType ) ).ToList();
        }

        /// <summary>
        /// 指定枠へ指定スキルを仮に装備します。既に別のスキルが入っていた場合はそのスキルの
        /// 所持数を1戻し、新しいスキルの所持数を1消費します。在庫がない場合や、枠の種類に合わない
        /// スキル(アクティブ枠へのパッシブスキル等)の場合は何もせずfalseを返します。
        /// </summary>
        public bool EquipSkill( SkillSlotType slotType, int slotIndex, SkillID newSkillID )
        {
            if ( !SkillsData.IsSkillMatchSlotType( newSkillID, slotType ) ) { return false; }
            if ( GetTentativeCount( newSkillID ) <= 0 ) { return false; }

            var slots = GetSlots( slotType );
            var oldSkillID = slots[slotIndex];
            if ( SkillsData.IsValidSkill( oldSkillID ) )
            {
                _tentativeCounts[oldSkillID] = GetTentativeCount( oldSkillID ) + 1;
            }

            _tentativeCounts[newSkillID] = GetTentativeCount( newSkillID ) - 1;
            slots[slotIndex] = newSkillID;

            return true;
        }

        /// <summary>
        /// 指定枠の装備を仮に解除します。装備されていたスキルの所持数を1戻します。
        /// 元々未装備だった場合は何もせずfalseを返します。
        /// </summary>
        public bool UnequipSkill( SkillSlotType slotType, int slotIndex )
        {
            var slots = GetSlots( slotType );
            var oldSkillID = slots[slotIndex];
            if ( !SkillsData.IsValidSkill( oldSkillID ) ) { return false; }

            _tentativeCounts[oldSkillID] = GetTentativeCount( oldSkillID ) + 1;
            slots[slotIndex] = SkillID.NONE;

            return true;
        }

        /// <summary>
        /// 仮の装備構成・所持数をCharacter.Status.EquipSkills/PassiveSkills/UserDomain.SkillInventoryへ反映します。
        /// </summary>
        public void Commit()
        {
            ref var status = ref Character.GetStatusRef;
            status.EnsureSkillSlots();
            for ( int i = 0; i < _tentativeActiveSkills.Length; ++i )
            {
                status.EquipSkills[i] = _tentativeActiveSkills[i];
            }
            for ( int i = 0; i < _tentativePassiveSkills.Length; ++i )
            {
                status.PassiveSkills[i] = _tentativePassiveSkills[i];
            }

            foreach ( var kvp in _tentativeCounts )
            {
                int original = _originalCounts.TryGetValue( kvp.Key, out var value ) ? value : 0;
                int delta = kvp.Value - original;
                if ( delta != 0 )
                {
                    _userDomain.AddSkill( kvp.Key, delta );
                }
            }

            Character.RefreshUseableSkillFlags( SituationType.NONE, 0xff );
        }

        private SkillID[] GetSlots( SkillSlotType slotType )
        {
            return ( slotType == SkillSlotType.PASSIVE ) ? _tentativePassiveSkills : _tentativeActiveSkills;
        }

        /// <summary>
        /// 装備枠を複製します。パッシブ枠追加以前のデータ等で枠が未確保・枠数不一致の場合も、
        /// 指定の枠数に揃えて(不足分はSkillID.NONEで補完して)返します。
        /// </summary>
        static private SkillID[] CopySlots( SkillID[] source, int slotNum )
        {
            var slots = new SkillID[slotNum];
            for ( int i = 0; i < slotNum; ++i )
            {
                slots[i] = ( source != null && i < source.Length ) ? source[i] : SkillID.NONE;
            }

            return slots;
        }
    }
}
