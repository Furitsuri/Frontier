using Frontier.Entities;
using Frontier.Tutorial;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        public void RegistSelfBuffs( Character owner )
        {
            var factory = _hierarchyBld.InstantiateWithDiContainer<SelfBuffSequenceCreator>( false );
            var param   = _hierarchyBld.InstantiateWithDiContainer<SelfBuffSequenceParam>( false );
            param.Self  = owner;

            ISequence selfBuffSeq = factory.CreateSequence( param );

            _handler.Regist( selfBuffSeq );
        }

        public bool IsEmptySequence()
        {
            return _handler.GetSequencesCount() == 0;
        }
    }
}
