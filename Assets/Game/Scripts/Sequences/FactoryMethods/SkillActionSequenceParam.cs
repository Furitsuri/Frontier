using Frontier.Entities;
using Frontier.Combat;
using System.Collections.Generic;

namespace Frontier.Sequences
{
    public class SkillActionSequenceParam
    {
        public SkillID SkillID;
        public SkillActionBase SkillAction;
        public Character Attacker;
        public Character Target;
        public List<CharacterKey> AttackTargetCharaKeys;
    }
}