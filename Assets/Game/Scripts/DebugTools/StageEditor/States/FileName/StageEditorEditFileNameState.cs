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

            // 入力ガイドを登録(InputField側で処理が行われるため、基本的に入力を受け取っても何もしない。ガイド表示のみ)
            _inputFcd.RegisterInputCodes(
                ( GuideIcon.CONFIRM, "Choose Candidate", CanAcceptConfirm, new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode ),
                ( GuideIcon.CANCEL, "Back", CanAcceptInputAlways, new AcceptBooleanInput( AcceptCancel ), 0.0f, hashCode )
            );
        }

        protected override bool CanAcceptConfirm()
        {
            return _uiSystem.DebugUi.StageEditorView.HasSuggestions();
        }
    }
}
#endif //UNITY_EDITOR