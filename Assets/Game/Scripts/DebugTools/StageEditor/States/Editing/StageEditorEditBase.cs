using Frontier.DebugTools.StageEditor;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.DebugTools
{
    public class StageEditorEditBase
    {
        protected GridCursorController _gridCursorCtrl        = null;
        protected StageEditorController.RefParams _refParams  = null;

        protected Action<int, int> PlaceTileCallback;
        protected Action<int, int> ResizeTileGridCallback;

        [Inject]
        private void Construct( GridCursorController gridCursorCtrl, StageEditorController.RefParams refParams )
        {
            _gridCursorCtrl = gridCursorCtrl;
            _refParams      = refParams;
        }

        virtual public void Init( Action<int, int> placeTileCb, Action<int, int> resizeTileGridCb )
        {
            PlaceTileCallback       = placeTileCb;
            ResizeTileGridCallback  = resizeTileGridCb;
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