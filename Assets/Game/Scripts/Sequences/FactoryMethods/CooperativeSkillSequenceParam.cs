using Frontier.Combat;
using System.Collections.Generic;

namespace Frontier.Sequences
{
    public class CooperativeSkillEntry
    {
        public SkillID SkillID;
        public SkillActionBase SkillAction;
    }

    public class CooperativeSkillSequenceParam
    {
        public List<CooperativeSkillEntry> Entries;
    }
}
