
using Frontier.Entities;
using Frontier.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.FormTroop
{
    public class RecruitConfirmCompletedState : ConfirmPhaseStateBase
    {
        protected RecruitPhasePresenter _presenter = null;

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as RecruitPhasePresenter;
        }

        public override void Init()
        {
            base.Init();

            _presenter.InitEmploymentCompletedUI( AssignConfirmUI );
        }

        public override bool Update()
        {
            return base.Update();
        }

        public override void ExitState()
        {
            base.ExitState();
        }

        protected override bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) { return false; }

            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                _isEndedPhase = true;
            }

            Back();

            return true;
        }
    }
}