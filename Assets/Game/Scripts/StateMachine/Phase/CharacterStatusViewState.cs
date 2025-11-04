namespace Frontier.StateMachine
{
    /// <summary>
    /// キャラクターステータス表示状態
    /// </summary>
    public class CharacterStatusViewState : PhaseStateBase
    {
        override public void Init()
        {
            base.Init();

            // _uiSystem.DeployUi.SetActiveConfirmUis( true );
        }

        override public bool Update()
        {
            if( base.Update() )
            {
                return true;
            }

            return IsBack();
        }

        override public void ExitState()
        {
            // _uiSystem.DeployUi.SetActiveConfirmUis( false );

            base.ExitState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.CANCEL, "BACK", CanAcceptDefault, new AcceptBooleanInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// /// <returns>決定入力の有無</returns>
        override protected bool AcceptCancel( bool isInput )
        {
            if( !isInput ) return false;

            Back();

            return true;
        }
    }
}