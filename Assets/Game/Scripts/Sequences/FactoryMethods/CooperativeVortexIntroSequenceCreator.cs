using Frontier;
using Zenject;

namespace Frontier.Sequences
{
    public class CooperativeVortexIntroSequenceCreator : SequenceCreator<CooperativeVortexIntroSequenceParam>
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        public override ISequence CreateSequence( CooperativeVortexIntroSequenceParam param )
        {
            object[] args = new object[] { param.Participants };
            return _hierarchyBld.InstantiateWithDiContainer<CooperativeVortexIntroSequence>( args, false );
        }
    }
}
