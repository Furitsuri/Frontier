using Frontier.Entities;
using Frontier.Presenter;
using static Constants;

namespace Frontier.StateMachine
{
    /// <summary>
    /// キャラクターステータス表示状態
    /// </summary>
    public class CharacterStatusViewState : PhaseStateBase
    {
        private string[] _inputInfoStrings;
        private Character _targetChara = null;
        private StatusPresenter _statusPresenter = null;
        private InputCodeStringWrapper _inputInfoStrWrapper = null;

        /// <summary>
        /// ステータス表示対象のキャラクターを割り当てます
        /// </summary>
        private Character GetContextAsCharacter()
        {
            object obj;
            Handler.SendContext( out obj );
            return obj as Character;
        }

        private void AssignCharacter()
        {
            _targetChara = GetContextAsCharacter();
            NullCheck.AssertNotNull( _targetChara, nameof( _targetChara ) );
            _statusPresenter.OpenCharacterStatus( _targetChara );
        }

        override public void Init()
        {
            base.Init();

            if( null == _statusPresenter )
            {
                _statusPresenter = _hierarchyBld.InstantiateWithDiContainer<StatusPresenter>( false );
                NullCheck.AssertNotNull( _statusPresenter, nameof( _statusPresenter ) );
            }
            _statusPresenter.Init();

            // INFOアイコンの文字列を設定
            _inputInfoStrings = new string[]
            {
                "SHOW TOOL TIP", // ツールチップ表示
                "HIDE TOOL TIP", // ツールチップ非表示
            };

            _inputInfoStrWrapper = new InputCodeStringWrapper( _inputInfoStrings[0] );
        }

        override public bool Update()
        {
            // INFOアイコンの文字列を更新
            _inputInfoStrWrapper.Explanation = _statusPresenter.IsToolTipActive() ? _inputInfoStrings[1] : _inputInfoStrings[0];

            return ( 0 <= TransitIndex );
        }

        override public void RunState()
        {
            base.RunState();

            AssignCharacter();
        }

        override public void RestartState()
        {
            base.RestartState();

            AssignCharacter();
        }

        override public void ExitState()
        {
            base.ExitState();

            _statusPresenter.CloseCharacterStatus();
            _targetChara = null;
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
                ( GuideIcon.ALL_CURSOR, "SELECT",               CanAcceptDirection, new AcceptDirectionInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
                ( GuideIcon.CANCEL,     "BACK",                 CanAcceptDefault, new AcceptBooleanInput( AcceptCancel ), 0.0f, hashCode ),
                ( GuideIcon.INFO,       _inputInfoStrWrapper,   CanAcceptDefault, new AcceptBooleanInput( AcceptInfo ),   0.0f, hashCode )
            );
        }

        /// <summary>
        /// ツールチップUIを表示しているときに限り、方向入力を受け付けます
        /// </summary>
        /// <returns></returns>
        override protected bool CanAcceptDirection()
        {
            return _statusPresenter.IsToolTipActive();
        }

        override protected bool AcceptDirection( Direction dir )
        {
            // TODO : ツールチップUIの操作処理を実装

            return false;
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

        override protected bool AcceptInfo( bool isInput )
        {
            if( !isInput ) return false;

            _statusPresenter.ToggleToolTipActive();

            return true;
        }
    }
}