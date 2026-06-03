using Frontier;
using Zenject;

namespace Frontier.Sequences
{
    public class CooperativeSkillSequenceCreator : SequenceCreator<CooperativeSkillSequenceParam>
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        public override ISequence CreateSequence( CooperativeSkillSequenceParam param )
        {
            object[] args = new object[] { param.Entries };
            return _hierarchyBld.InstantiateWithDiContainer<CooperativeSkillSequence>( args, false );
        }
    }
}
