using Frontier.Stage;
using System;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorEditBase
    {
        [Inject] protected GridCursor _gridCursor                 = null;
        [Inject] protected StageEditorController.StageEditRefParams _refParams  = null;

        protected EditActionContext _context;
        protected Action<EditActionContext> OwnCallback;

        /// <summary>サブモード変更時などに入力コードの再登録を要求するコールバック</summary>
        public Action RefreshInputCodes = null;

        /// <summary>Confirm 入力でアクションが完了した際に呼ばれるコールバック</summary>
        public Action OnCompleted = null;

        virtual public void Init( Action<EditActionContext> callback )
        {
            _context    = new EditActionContext();
            OwnCallback = callback;

            _context.Setup();
        }

        virtual public void Exit()
        {

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

        /// <summary>Sub1/Sub2 ガイドラベル。null の場合は登録しない。</summary>
        virtual public string GetSub12Label() { return null; }

        /// <summary>Sub3/Sub4 ガイドラベル。null の場合は登録しない。</summary>
        virtual public string GetSub34Label() { return null; }
    }
}

#endif // UNITY_EDITOR