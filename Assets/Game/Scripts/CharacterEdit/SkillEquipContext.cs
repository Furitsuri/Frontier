using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// 装備スキル設定画面における「仮の割り振り」状態を保持します。
    /// 実際のCharacter.Status.EquipSkills/UserDomain.SkillInventoryはここでは一切書き換えず、
    /// 決定(OK)操作が行われた際にSkillEquipHandlerが本データへ反映します
    /// (それまでは全て仮の値であり、キャンセルすれば破棄されます)。
    /// </summary>
    public class SkillEquipContext
    {
        public Character Character { get; }

        private readonly UserDomain _userDomain;
        private readonly SkillID[] _tentativeEquipSkills;
        private readonly Dictionary<SkillID, int> _originalCounts;
        private readonly Dictionary<SkillID, int> _tentativeCounts;

        public SkillEquipContext( Character character, UserDomain userDomain )
        {
            Character = character;
            _userDomain = userDomain;

            var status = character.GetStatusRef;
            _tentativeEquipSkills = ( SkillID[] ) status.EquipSkills.Clone();

            _originalCounts = new Dictionary<SkillID, int>();
            foreach ( var entry in userDomain.SkillInventory )
            {
                _originalCounts[entry.SkillID] = entry.Count;
            }

            // 現在装備中のスキルが所持数リストに存在しない場合(初期装備等)も一覧に表示できるよう、
            // 0個所持として補完しておく
            foreach ( var skillID in _tentativeEquipSkills )
            {
                if ( !SkillsData.IsValidSkill( skillID ) ) { continue; }
                if ( !_originalCounts.ContainsKey( skillID ) ) { _originalCounts[skillID] = 0; }
            }

            _tentativeCounts = new Dictionary<SkillID, int>( _originalCounts );
        }

        /// <summary>
        /// 指定枠(0〜EQUIPABLE_SKILL_MAX_NUM-1)に仮に装備されているスキルを返します。
        /// </summary>
        public SkillID GetEquippedSkill( int slotIndex ) => _tentativeEquipSkills[slotIndex];

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
        /// 指定枠へ指定スキルを仮に装備します。既に別のスキルが入っていた場合はそのスキルの
        /// 所持数を1戻し、新しいスキルの所持数を1消費します。在庫がない場合は何もせずfalseを返します。
        /// </summary>
        public bool EquipSkill( int slotIndex, SkillID newSkillID )
        {
            if ( GetTentativeCount( newSkillID ) <= 0 ) { return false; }

            var oldSkillID = _tentativeEquipSkills[slotIndex];
            if ( SkillsData.IsValidSkill( oldSkillID ) )
            {
                _tentativeCounts[oldSkillID] = GetTentativeCount( oldSkillID ) + 1;
            }

            _tentativeCounts[newSkillID] = GetTentativeCount( newSkillID ) - 1;
            _tentativeEquipSkills[slotIndex] = newSkillID;

            return true;
        }

        /// <summary>
        /// 指定枠の装備を仮に解除します。装備されていたスキルの所持数を1戻します。
        /// 元々未装備だった場合は何もせずfalseを返します。
        /// </summary>
        public bool UnequipSkill( int slotIndex )
        {
            var oldSkillID = _tentativeEquipSkills[slotIndex];
            if ( !SkillsData.IsValidSkill( oldSkillID ) ) { return false; }

            _tentativeCounts[oldSkillID] = GetTentativeCount( oldSkillID ) + 1;
            _tentativeEquipSkills[slotIndex] = SkillID.NONE;

            return true;
        }

        /// <summary>
        /// 仮の装備構成・所持数をCharacter.Status.EquipSkills/UserDomain.SkillInventoryへ反映します。
        /// </summary>
        public void Commit()
        {
            ref var status = ref Character.GetStatusRef;
            for ( int i = 0; i < _tentativeEquipSkills.Length; ++i )
            {
                status.EquipSkills[i] = _tentativeEquipSkills[i];
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
    }
}
