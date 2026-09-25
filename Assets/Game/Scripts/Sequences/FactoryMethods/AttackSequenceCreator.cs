using Frontier;
using Frontier.Option;
using Zenject;

namespace Frontier.Sequences
{
    public class AttackSequenceCreator : SequenceCreator<AttackSequenceParam>
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private OptionHandler _optionHandler = null;

        /// <summary>
        /// オプション設定に応じて、寄りの演出で見せる攻撃シーケンスか、ステージ上でそのまま完結する攻撃シーケンスを生成します
        /// </summary>
        public override ISequence CreateSequence( AttackSequenceParam param )
        {
            object[] args = new object[] { param.Attacker, param.Target };

            if( _optionHandler.IsAttackCloseUpEnabled )
            {
                return _hierarchyBld.InstantiateWithDiContainer<CloseUpAttackSequence>( args, false );
            }

            return _hierarchyBld.InstantiateWithDiContainer<InPlaceAttackSequence>( args, false );
        }
    }
}
