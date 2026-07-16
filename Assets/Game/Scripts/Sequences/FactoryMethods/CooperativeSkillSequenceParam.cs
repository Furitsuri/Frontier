using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;

namespace Frontier.Sequences
{
    public class CooperativeSkillEntry
    {
        public SkillID SkillID;
        public SkillActionBase SkillAction;
        public Character Attacker;
        public Character Target;
        public List<CharacterKey> AttackTargetCharaKeys;
    }

    public class CooperativeSkillSequenceParam
    {
        public List<CooperativeSkillEntry> Entries;
    }
}
