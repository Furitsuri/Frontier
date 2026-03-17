using Zenject;

namespace Frontier.Sequences
{
    public class SelfBuffSequenceCreator : SequenceCreator<SelfBuffSequenceParam>
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        public override ISequence CreateSequence( SelfBuffSequenceParam param )
        {
            object[] args = { param.Self, param.cmdName };

            return _hierarchyBld.InstantiateWithDiContainer<SelfBuffSequence>( args, false );
        }
    }
}