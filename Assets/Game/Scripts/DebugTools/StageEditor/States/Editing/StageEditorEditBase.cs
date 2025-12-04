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

        virtual public bool AcceptConfirm( bool isInput ) { return false; }

        virtual public bool AcceptCancel( bool isCancel ) { return false; }

        virtual public bool AcceptSub1( bool isInput ) { return false; }

        virtual public bool AcceptSub2( bool isInput ) { return false; }

        virtual public bool AcceptSub3( bool isInput ) { return false; }

        virtual public bool AcceptSub4( bool isInput ) { return false; }
    }
}

#endif // UNITY_EDITOR