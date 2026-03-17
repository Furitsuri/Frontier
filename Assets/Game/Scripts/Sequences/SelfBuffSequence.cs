using Frontier.Battle;
using Frontier.Entities;
using Frontier.Registries;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Sequences
{
    public class SelfBuffSequence : ISequence
    {
        private enum State
        {
            DISP_BUFF_NAME = 0,
            EXE_BUFF,
        }

        [Inject] private BattleRoutinePresenter _presenter = null; 

        private float _elapsedTime = 0f;
        private string _commandName;
        private State _state;
        private Character _owner;

        public SelfBuffSequence( Character owner, string cmdName )
        {
            _owner          = owner;
            _commandName    = cmdName;
        }

        public void Start()
        {
            _state = State.DISP_BUFF_NAME;
            ResetElapsedTime();
            _presenter.SetCommandName( _commandName );
            _presenter.SetActiveCommandName( true );
        }

        public void End()
        {
            _presenter.SetActiveCommandName( false );
        }

        public bool Update()
        {
            _elapsedTime += Time.deltaTime;

            switch( _state )
            {
                case State.DISP_BUFF_NAME:
                    if( SELF_BUFF_DISP_NAME_TIME <= _elapsedTime )
                    {
                        ResetElapsedTime();

                        _owner.BattleLogic.AddBuffEffect();

                        _state = State.EXE_BUFF;
                    }
                    break;
                case State.EXE_BUFF:
                    if( SELF_BUFF_EXE_TIME <= _elapsedTime )
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        private void ResetElapsedTime()
        {
            _elapsedTime = 0f;
        }
    }
}