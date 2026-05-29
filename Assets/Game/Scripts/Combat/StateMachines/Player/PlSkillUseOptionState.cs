using Frontier.Combat;
using System.Collections.Generic;
using static Constants;

namespace Frontier.Battle
{
    public class PlSkillUseOptionState : PlPhaseStateBase, ICommandCursorProvider
    {
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        public USE_SKILL_OPTION_TAG SelectedOption { get; private set; } = USE_SKILL_OPTION_TAG.NONE;

        public override void Init( object context )
        {
            base.Init( context );

            SelectedOption = USE_SKILL_OPTION_TAG.NONE;

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );

            var commandIndices = new List<int>();
            for( int i = 0; i < ( int ) USE_SKILL_OPTION_TAG.NUM; ++i )
            {
                commandIndices.Add( i );
            }
            _commandList.Init( ref commandIndices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _presenter.InitUseSkillOptionView( this );
        }

        public override bool Update()
        {
            if( base.Update() ) { return true; }

            return false;
        }

        public override object ExitState()
        {
            _presenter.ExitUseSkillOptionView();

            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.VERTICAL_CURSOR, "Select",  CanAcceptDefault, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM,         "Confirm", CanAcceptDefault, new AcceptContextInput( AcceptConfirm ),   0.0f, hashCode),
                (GuideIcon.CANCEL,          "Back",    CanAcceptDefault, new AcceptContextInput( AcceptCancel ),    0.0f, hashCode)
            );
        }

        public int GetCurrentIndex() => _cmdIdxVal.index;

        protected override bool AcceptDirection( InputContext context )
        {
            return _commandList.OperateListCursor( context.Cursor );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            SelectedOption = ( USE_SKILL_OPTION_TAG ) _cmdIdxVal.value;
            Back();

            return true;
        }
    }
}
