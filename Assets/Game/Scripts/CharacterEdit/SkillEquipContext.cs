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
        /// 仮の装備構成における、パッシブスキルの装備コストの合計です。
        /// </summary>
        public int TentativePassiveSkillsCost => PassiveSkillsData.GetTotalCost( Passive.Slots );

        /// <summary>
        /// 仮の装備構成における、実際のスキルゲージ最大値です(素の最大値 - パッシブスキルの装備コストの合計)。
        /// </summary>
        public int TentativeMaxSkillGauge => Status.CalcEffectiveMaxSkillGauge( Character.GetStatusRef.maxSkillGauge, TentativePassiveSkillsCost );

        /// <summary>
        /// 指定枠のパッシブスキルを指定スキルへ差し替えた(NONEの場合は外した)場合の、実際のスキルゲージ最大値を返します。
        /// </summary>
        public int GetMaxSkillGaugeAfterPassiveChange( int slotIndex, PassiveSkillID newSkillID )
        {
            int cost = TentativePassiveSkillsCost
                - PassiveSkillsData.GetCost( Passive.GetEquippedSkill( slotIndex ) )
                + PassiveSkillsData.GetCost( newSkillID );

            return Status.CalcEffectiveMaxSkillGauge( Character.GetStatusRef.maxSkillGauge, cost );
        }

        /// <summary>
        /// 仮の装備構成で、指定枠のアクティブスキルがスキルゲージ最大値の範囲内で使用出来るかを返します(未装備枠はtrue)。
        /// </summary>
        public bool IsActiveSlotWithinMaxSkillGauge( int slotIndex )
        {
            return ActiveSkillsData.IsCostWithinMaxSkillGauge( Active.GetEquippedSkill( slotIndex ), TentativeMaxSkillGauge );
        }

        /// <summary>
        /// 仮の装備構成で、スキルゲージ最大値が使用コストを下回っていて使用出来ないアクティブスキルを返します。
        /// </summary>
        public List<ActiveSkillID> GetActiveSkillsOverMaxSkillGauge()
        {
            var result = new List<ActiveSkillID>();
            for ( int i = 0; i < Active.SlotCount; ++i )
            {
                if ( !IsActiveSlotWithinMaxSkillGauge( i ) ) { result.Add( Active.GetEquippedSkill( i ) ); }
            }

            return result;
        }

        /// <summary>
        /// 指定枠のパッシブスキルを指定スキルへ差し替えた(NONEの場合は外した)場合に、新たに使用出来なくなるアクティブスキルを返します。
        /// </summary>
        public List<ActiveSkillID> GetActiveSkillsBecomingUnusableByPassiveChange( int slotIndex, PassiveSkillID newSkillID )
        {
            int maxAfter = GetMaxSkillGaugeAfterPassiveChange( slotIndex, newSkillID );

            var result = new List<ActiveSkillID>();
            for ( int i = 0; i < Active.SlotCount; ++i )
            {
                var skillID = Active.GetEquippedSkill( i );
                if ( IsActiveSlotWithinMaxSkillGauge( i ) && !ActiveSkillsData.IsCostWithinMaxSkillGauge( skillID, maxAfter ) )
                {
                    result.Add( skillID );
                }
            }

            return result;
        }

        /// <summary>
        /// 指定アクティブスキルが、仮の装備構成でのスキルゲージ最大値の範囲内で使用出来るかを返します。
        /// </summary>
        public bool IsActiveSkillWithinMaxSkillGauge( ActiveSkillID skillID )
        {
            return ActiveSkillsData.IsCostWithinMaxSkillGauge( skillID, TentativeMaxSkillGauge );
        }

        /// <summary>
        /// 指定パッシブスキルを指定枠へ装備出来るかを返します。SkillEquipSlotGroup.CanEquipの条件に加え、
        /// 装備コストの合計が素のスキルゲージ最大値を超える場合は装備出来ません(枠に既に入っているスキルのコストは差し替えとして差し引きます)。
        /// </summary>
        public bool CanEquipPassive( int slotIndex, PassiveSkillID skillID )
        {
            if ( !Passive.CanEquip( slotIndex, skillID ) ) { return false; }

            int costAfterEquip = TentativePassiveSkillsCost
                - PassiveSkillsData.GetCost( Passive.GetEquippedSkill( slotIndex ) )
                + PassiveSkillsData.GetCost( skillID );

            return costAfterEquip <= Character.GetStatusRef.maxSkillGauge;
        }

        /// <summary>
        /// 指定枠へ指定パッシブスキルを仮に装備します。装備出来ない場合(CanEquipPassive参照)は何もせずfalseを返します。
        /// </summary>
        public bool EquipPassive( int slotIndex, PassiveSkillID skillID )
        {
            if ( !CanEquipPassive( slotIndex, skillID ) ) { return false; }

            return Passive.Equip( slotIndex, skillID );
        }

        /// <summary>
        /// 仮の装備構成・所持数をCharacter.Status.EquipSkills/PassiveSkills/UserDomainの所持スキルへ反映します。
        /// </summary>
        public void Commit()
        {
            ref var status = ref Character.GetStatusRef;
            status.EnsureSkillSlots();

            // 装備変更前にゲージが満タンだった場合は、変更後も満タンとする(外した分だけ最大値と共に現在値も戻す)
            bool wasGaugeFull = status.EffectiveMaxSkillGauge <= status.CurSkillGauge;

            Active.CommitTo( status.EquipSkills, ( id, delta ) => _userDomain.AddSkill( id, delta ) );
            Passive.CommitTo( status.PassiveSkills, ( id, delta ) => _userDomain.AddSkill( id, delta ) );

            int maxSkillGauge      = status.EffectiveMaxSkillGauge;
            status.CurSkillGauge   = wasGaugeFull ? maxSkillGauge : UnityEngine.Mathf.Min( status.CurSkillGauge, maxSkillGauge );

            Character.RefreshModifiedParam();
            Character.RefreshUseableSkillFlags( SituationType.NONE, 0xff );
        }
    }
}
