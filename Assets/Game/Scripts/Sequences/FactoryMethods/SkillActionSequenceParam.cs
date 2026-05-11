using Frontier.Entities;
using Frontier.Combat;

namespace Frontier.Sequences
{
    public class SkillActionSequenceParam
    {
        public SkillID SkillID;
        public SkillActionBase SkillAction;
        public Character Attacker;
        public Character Target;
    }
}