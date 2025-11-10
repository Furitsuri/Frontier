using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.StateMachine
{
    public class DeploymentPhaseStateBase : PhaseStateBase
    {
        protected DeploymentPhasePresenter _presenter = null;

        public void AssignPresenter( DeploymentPhasePresenter presenter )
        {
            _presenter = presenter;
        }
    }
}