using Frontier.Stage;
using Frontier.StateMachine;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// 配置フェーズ：配置確定終了状態
    /// </summary>
    public class DeploymentConfirmCompletedState : ConfirmPhaseStateBase
    {
        protected override bool AcceptConfirm( InputContext context )
        {
			if( !base.AcceptConfirm( context ) ) { return false; }

			// 配置完了を確定させて配置フェーズを終了する
			if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                _isEndedPhase = true;
            }

            Back();

            return true;
        }
    }
}