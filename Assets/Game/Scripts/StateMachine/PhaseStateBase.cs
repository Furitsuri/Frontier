using Frontier.Tutorial;
using UnityEngine;
using Zenject;

namespace Frontier.StateMachine
{
    public class PhaseStateBase : StateBase
    {
        [Inject] protected IUiSystem _uiSystem = null;
        [Inject] protected HierarchyBuilderBase _hierarchyBld = null;
        [Inject] protected TutorialFacade _tutorialFcd = null;

        protected bool _isEndedPhase = false;
        protected PhaseHandlerBase Handler { get; private set; }
        public bool IsEndedPhase { get { return _isEndedPhase; } }

        public void AssignHandler( PhaseHandlerBase handler )
        {
            Handler = handler;
        }

        /// <summary>
        /// 入力コードのハッシュ値を取得します
        /// </summary>
        /// <returns>入力コードのハッシュ値</returns>
        protected int GetInputCodeHash()
        {
            return Hash.GetStableHash( GetType().Name );
        }

        virtual public void AssignPresenter( PhasePresenterBase presenter ) { }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        virtual public void RegisterInputCodes() { }

        /// <summary>
        /// 指定するハッシュ値の入力コードを登録解除します
        /// </summary>
        /// <param name="hashCode">ハッシュ値</param>
        virtual public void UnregisterInputCodes( int hashCode )
        {
            _inputFcd.UnregisterInputCodes( hashCode );
        }

        /// <summary>
        /// 入力を受付るかを取得します
        /// 多くのケースでこちらの関数を用いて判定します
        /// </summary>
        /// <returns>入力受付の可否</returns>
        virtual protected bool CanAcceptDefault()
        {
            // 現在のステートから脱出する場合は入力を受け付けない
            return !IsBack();
        }

        /*
         *  以下、入力受付可否関数と入力受付関数
         */
        virtual protected bool CanAcceptDirection() { return false; }
        virtual protected bool CanAcceptConfirm() { return false; }
        virtual protected bool CanAcceptCancel() { return false; }
        virtual protected bool CanAcceptOptional() { return false; }
        virtual protected bool CanAcceptTool() { return false; }
        virtual protected bool CanAcceptInfo() { return false; }
        virtual protected bool CanAcceptSub1() { return false; }
        virtual protected bool CanAcceptSub2() { return false; }
        virtual protected bool CanAcceptSub3() { return false; }
        virtual protected bool CanAcceptSub4() { return false; }

        virtual protected bool AcceptDirection( InputContext context )  { return false; }
        virtual protected bool AcceptConfirm( InputContext context )    { return context.GetButton( GameButton.Confirm ); }
        virtual protected bool AcceptCancel( InputContext context )     { return context.GetButton( GameButton.Cancel ); }
        virtual protected bool AcceptTool( InputContext context )       { return context.GetButton( GameButton.Tool ); }
        virtual protected bool AcceptInfo( InputContext context )       { return context.GetButton( GameButton.Info ); }
        virtual protected bool AcceptOpt1( InputContext context )       { return context.GetButton( GameButton.Opt1 ); }
        virtual protected bool AcceptOpt2( InputContext context )       { return context.GetButton( GameButton.Opt2 ); }
        virtual protected bool AcceptSub1( InputContext context )       { return context.GetButton( GameButton.Sub1 ); }
        virtual protected bool AcceptSub2( InputContext context )       { return context.GetButton( GameButton.Sub2 ); }
        virtual protected bool AcceptSub3( InputContext context )       { return context.GetButton( GameButton.Sub3 ); }
        virtual protected bool AcceptSub4( InputContext context )       { return context.GetButton( GameButton.Sub4 ); }
        virtual protected bool AcceptCamera( Vector2 vec ) { return false; }

        /// <summary>
        /// 現在のステートを実行します
        /// </summary>
        public override void RunState()
        {
            base.RunState();
            RegisterInputCodes();
        }

        /// <summary>
        /// 現在のステートを再開します
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();
            RegisterInputCodes();
        }

        /// <summary>
        /// 現在のステートを中断します
        /// </summary>
        public override void PauseState()
        {
            base.PauseState();
            UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );
        }

        /// <summary>
        /// 現在のステートから退避します
        /// </summary>
        public override void ExitState()
        {
            base.ExitState();
            UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );

            // 表示すべきチュートリアルがある場合はチュートリアル遷移に移行
            _tutorialFcd.TryShowTutorial();
        }
    }
}