using Frontier;
using Zenject;

namespace Frontier.Sequences
{
    public class SkillActionSequenceCreator : SequenceCreator<SkillActionSequenceParam>
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        public override ISequence CreateSequence( SkillActionSequenceParam param )
        {
            object[] args = new object[] { param.Attacker, param.Target };

            return _hierarchyBld.InstantiateWithDiContainer<SkillActionSequence>( args, false );
        }
    }
}