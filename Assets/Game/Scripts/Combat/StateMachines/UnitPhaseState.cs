using Frontier.Stage;
using Frontier.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Battle
{
    public class UnitPhaseState : PhaseStateBase
    {
        [Inject] protected BattleRoutineController _btlRtnCtrl  = null;
        [Inject] protected StageController _stageCtrl           = null;

        /// <summary>
        /// 死亡したキャラクターの存在を通知します
        /// </summary>
        /// <param name="characterKey">死亡したキャラクターのハッシュキー</param>
        protected void NorifyCharacterDied( in CharacterKey characterKey )
        {
            _btlRtnCtrl.BtlCharaCdr.SetDiedCharacterKey( characterKey );
            _btlRtnCtrl.BtlCharaCdr.RemoveCharacterFromList( characterKey );
        }
    }
}