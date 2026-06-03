using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Sequences
{
    public class SequenceFacade
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private SequenceHandler _handler = null;

        public void Setup( FocusRoutineBase handler )
        {
            LazyInject.GetOrCreate( ref _handler, () => handler as SequenceHandler );
        }

        public void RegistAttack( Character attacker, Character target )
        {
            var factory     = _hierarchyBld.InstantiateWithDiContainer<AttackSequenceCreator>( false );
            var param       = _hierarchyBld.InstantiateWithDiContainer<AttackSequenceParam>( false );
            param.Attacker  = attacker;
            param.Target    = target;

            ISequence attackSeq = factory.CreateSequence( param );

            _handler.Regist( attackSeq );
        }

        public void RegistSkillAction( Character attacker, Character target, SkillID skillID, List<CharacterKey> attackTargetCharaKeys )
        {
            var factory         = _hierarchyBld.InstantiateWithDiContainer<SkillActionSequenceCreator>( false );
            var param           = _hierarchyBld.InstantiateWithDiContainer<SkillActionSequenceParam>( false );
            param.Attacker      = attacker;
            param.Target        = target;
            param.SkillID       = skillID;
            param.SkillAction   = SkillsData.CreateSkillAction( skillID, attacker, attackTargetCharaKeys, _hierarchyBld );
            ISequence skillActionSeq = factory.CreateSequence( param );
            _handler.Regist( skillActionSeq );
        }

        public void RegistSelfBuffs( Character owner, string cmdName )
        {
            var factory     = _hierarchyBld.InstantiateWithDiContainer<SelfBuffSequenceCreator>( false );
            var param       = _hierarchyBld.InstantiateWithDiContainer<SelfBuffSequenceParam>( false );
            param.Self      = owner;
            param.cmdName   = cmdName;

            ISequence selfBuffSeq = factory.CreateSequence( param );

            _handler.Regist( selfBuffSeq );
        }

        /// <summary>
        /// 連携スキルの 1 エントリを生成します。
        /// </summary>
        public CooperativeSkillEntry CreateCooperativeEntry( SkillID skillID, Character attacker, List<CharacterKey> attackTargetCharaKeys )
        {
            return new CooperativeSkillEntry
            {
                SkillID     = skillID,
                SkillAction = SkillsData.CreateSkillAction( skillID, attacker, attackTargetCharaKeys, _hierarchyBld ),
            };
        }

        /// <summary>
        /// 連携スキルシーケンスをハンドラに登録します。
        /// </summary>
        public void RegistCooperativeSkillAction( List<CooperativeSkillEntry> entries )
        {
            var factory   = _hierarchyBld.InstantiateWithDiContainer<CooperativeSkillSequenceCreator>( false );
            var param     = _hierarchyBld.InstantiateWithDiContainer<CooperativeSkillSequenceParam>( false );
            param.Entries = entries;
            _handler.Regist( factory.CreateSequence( param ) );
        }

        public bool IsEmptySequence()
        {
            return _handler.GetSequencesCount() == 0;
        }
    }
}
