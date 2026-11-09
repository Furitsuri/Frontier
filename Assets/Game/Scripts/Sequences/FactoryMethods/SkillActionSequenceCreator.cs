using Frontier;
using Frontier.Entities;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Sequences
{
    public class SkillActionSequenceCreator : SequenceCreator<SkillActionSequenceParam>
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        public override ISequence CreateSequence( SkillActionSequenceParam param )
        {
            // MEMO : DIコンテナへ渡す引数は型で照合されるため、nullを渡さないようにする
            var targetKeys = param.AttackTargetCharaKeys ?? new List<CharacterKey>();
            object[] args = new object[] { param.SkillAction, param.SkillID, param.Attacker, targetKeys };

            return _hierarchyBld.InstantiateWithDiContainer<SkillActionSequence>( args, false );
        }
    }
}