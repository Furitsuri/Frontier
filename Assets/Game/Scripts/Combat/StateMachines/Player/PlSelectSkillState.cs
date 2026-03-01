using Frontier.Battle;
using Frontier.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Battle
{
    public class PlSelectSkillState : PlPhaseStateBase
    {
        protected override void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            LazyInject.GetOrCreate( ref _plOwner, () => _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player );
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.CANCEL, "BACK", CanAcceptCancel, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        protected override bool CanAcceptCancel()
        {
            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            _plOwner.BattleLogic.ResetUseSkills();

            return true;
        }
    }
}