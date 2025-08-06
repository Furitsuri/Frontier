using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier.DebugTools
{
    public class StageEditorSaveLoadState : EditorStateBase
    {
        protected Action ToggleViewCallback;
        protected Action<string> SetWordCallback;

        override public void RunState()
        {
            base.RunState();

            ToggleViewCallback();
        }

        override public void ExitState()
        {
            ToggleViewCallback();

            base.ExitState();
        }

        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (new GuideIcon[] { GuideIcon.CONFIRM }, "CONFIRM", CanAcceptDefault, new IAcceptInputBase[] { new AcceptBooleanInput(AcceptConfirm) }, 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// /// <returns>決定入力の有無</returns>
        override protected bool AcceptConfirm(bool isInput)
        {
            if (!isInput) return false;

            Back();

            return true;
        }

        public void SetCallbacks(Action toggleViewCallback, Action<string> setWordCallback)
        {
            ToggleViewCallback  = toggleViewCallback;
            SetWordCallback     = setWordCallback;
        }
    }
}