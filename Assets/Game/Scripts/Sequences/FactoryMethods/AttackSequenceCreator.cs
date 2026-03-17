using Frontier;
using Zenject;

namespace Frontier.Sequences
{
    public class AttackSequenceCreator : SequenceCreator<AttackSequenceParam>
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        public override ISequence CreateSequence( AttackSequenceParam param )
        {
            object[] args = new object[] { param.Attacker, param.Target };

            return _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>( args, false );
        }
    }
}