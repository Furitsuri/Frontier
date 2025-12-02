using Frontier.Stage;
using System;
using UnityEngine;
using static Constants;
using static InputCode;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorEditFileNameState : EditorStateBase
    {
        override public void Init()
        {
            base.Init( );

            _uiSystem.DebugUi.StageEditorView.OpenEditFileName( () => { Back(); } );
        }

        public override void ExitState()
        {
            _uiSystem.DebugUi.StageEditorView.CloseEditFileName();

            base.ExitState();
        }

        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.CANCEL, "Back", CanAcceptInputAlways, new AcceptBooleanInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// キャンセル入力をした際、InputField側で処理が行われるため、ステート側ではキャンセルを受け付けない
        /// </summary>
        /// <param name="isCancel"></param>
        /// <returns></returns>
        protected override bool AcceptCancel( bool isCancel )
        {
            return false;
        }
    }
}
#endif //UNITY_EDITOR