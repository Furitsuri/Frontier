using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// 装備スキル設定画面における「仮の割り振り」状態を保持します。
    /// 実際のCharacter.Status.EquipSkills/PassiveSkills/UserDomainの所持スキルはここでは一切書き換えず、
    /// 決定(OK)操作が行われた際にSkillEquipHandlerが本データへ反映します
    /// (それまでは全て仮の値であり、キャンセルすれば破棄されます)。
    /// アクティブスキルとパッシブスキルはIDの型・所持数ともに別々に管理するため、種類ごとのSkillEquipSlotGroupを持ちます。
    /// </summary>
    public class SkillEquipContext
    {
        public Character Character { get; }

        /// <summary>
        /// アクティブスキルの装備枠。同一スキル(同一系統)は1人1つまでしか装備出来ません。
        /// </summary>
        public SkillEquipSlotGroup<ActiveSkillID> Active { get; }

        /// <summary>
        /// パッシブスキルの装備枠。同一スキル(同一系統。強化段階が異なっても同じIDのもの)は1人1つまでしか装備出来ません。
        /// </summary>
        public SkillEquipSlotGroup<PassiveSkillID> Passive { get; }

        private readonly UserDomain _userDomain;

        public SkillEquipContext( Character character, UserDomain userDomain )
        {
            Character = character;
            _userDomain = userDomain;

            var status = character.GetStatusRef;

            Active = new SkillEquipSlotGroup<ActiveSkillID>(
                status.EquipSkills,
                Constants.EQUIPABLE_ACTIVE_SKILL_MAX_NUM,
                userDomain.ActiveSkillInventory.Select( e => new KeyValuePair<ActiveSkillID, int>( e.SkillID, e.Count ) ),
                ActiveSkillsData.IsValidSkill,
                ActiveSkillID.NONE );

            Passive = new SkillEquipSlotGroup<PassiveSkillID>(
                status.PassiveSkills,
                Constants.EQUIPABLE_PASSIVE_SKILL_MAX_NUM,
                userDomain.PassiveSkillInventory.Select( e => new KeyValuePair<PassiveSkillID, int>( e.SkillID, e.Count ) ),
                PassiveSkillsData.IsValidSkill,
                PassiveSkillID.NONE );
        }

        /// <summary>
        /// 仮の装備構成・所持数をCharacter.Status.EquipSkills/PassiveSkills/UserDomainの所持スキルへ反映します。
        /// </summary>
        public void Commit()
        {
            ref var status = ref Character.GetStatusRef;
            status.EnsureSkillSlots();

            Active.CommitTo( status.EquipSkills, ( id, delta ) => _userDomain.AddSkill( id, delta ) );
            Passive.CommitTo( status.PassiveSkills, ( id, delta ) => _userDomain.AddSkill( id, delta ) );

            Character.RefreshModifiedParam();
            Character.RefreshUseableSkillFlags( SituationType.NONE, 0xff );
        }
    }
}
