
using Frontier.Entities;
using Frontier.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.FormTroop
{
    public class RecruitConfirmCompletedState : ConfirmPhaseStateBase
    {
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