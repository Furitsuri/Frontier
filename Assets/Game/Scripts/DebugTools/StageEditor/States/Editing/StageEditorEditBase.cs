using Frontier.Stage;
using System;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorEditBase
    {
        [Inject] protected GridCursorController _gridCursorCtrl        = null;
        [Inject] protected StageEditorController.StageEditRefParams _refParams  = null;

        protected Action<int, int> OwnCallback;

        virtual public void Init( Action<int, int> callback )
        {
            OwnCallback = callback;
        }

        virtual public void Update()
        {

        }

        virtual public bool CanAcceptConfirm() { return false; }
        virtual public bool CanAcceptCancel() { return false; }
        virtual public bool CanAcceptSub1() { return false; }
        virtual public bool CanAcceptSub2() { return false; }
        virtual public bool CanAcceptSub3() { return false; }
        virtual public bool CanAcceptSub4() { return false; }

        virtual public bool AcceptConfirm( InputContext context )   { return context.GetButton( GameButton.Confirm ); }
        virtual public bool AcceptCancel( InputContext context )    { return context.GetButton( GameButton.Cancel ); }
        virtual public bool AcceptSub1( InputContext context )      { return context.GetButton( GameButton.Sub1 ); }
        virtual public bool AcceptSub2( InputContext context )      { return context.GetButton( GameButton.Sub2 ); }
        virtual public bool AcceptSub3( InputContext context )      { return context.GetButton( GameButton.Sub3 ); }
        virtual public bool AcceptSub4( InputContext context )      { return context.GetButton( GameButton.Sub4 ); }
    }
}

#endif // UNITY_EDITOR