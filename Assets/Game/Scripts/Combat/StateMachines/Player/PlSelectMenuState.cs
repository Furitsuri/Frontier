using Frontier.Combat;
using Frontier.Option;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// PlSelectTileStateのOPT2入力から遷移する、"Option"/"Turn End"の選択メニューです。
    /// "Option"を選んだ場合はOptionHandlerを起動し、"Turn End"を選んだ場合はIsTurnEndSelectedを立てて
    /// 呼び出し元(PlSelectTileState)にターン終了確認画面への遷移を委ねます。
    /// </summary>
    public class PlSelectMenuState : PlPhaseStateBase, ICommandCursorProvider
    {
        private enum Phase
        {
            SELECT_OPTION = 0,
            END,
        }

        [Inject] private OptionHandler _optionHandler = null;

        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;
        private Phase _phase;

        public bool IsTurnEndSelected { get; private set; }

        public override void Init( object context )
        {
            base.Init( context );

            _phase            = Phase.SELECT_OPTION;
            IsTurnEndSelected = false;

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );

            var options = GetOptions();
            var commandIndices = new List<int>();
            foreach( var option in options )
            {
                commandIndices.Add( ( int ) option );
            }
            _commandList.Init( ref commandIndices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _presenter.InitTileMenuOptionView( this, options );
        }

        public override bool Update()
        {
            if( base.Update() ) { return true; }

            if( _phase == Phase.END )
            {
                Back();
                return true;
            }

            return false;
        }

        public override object ExitState()
        {
            _presenter.ExitTileMenuOptionView();

            return base.ExitState();
        }

        // MEMO : このメニューはグリッド選択中のキャラクターに依存しないため、AdaptSelectPlayer()は基底クラス(PlPhaseStateBase)のno-opのままオーバーライドしない

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.VERTICAL_CURSOR, "SELECT",  CanAcceptDefault, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM,         "CONFIRM", CanAcceptDefault, new AcceptContextInput( AcceptConfirm ),   0.0f, hashCode),
                (GuideIcon.CANCEL,          "BACK",    CanAcceptDefault, new AcceptContextInput( AcceptCancel ),    0.0f, hashCode)
            );
        }

        /// <summary>
        /// 選択肢実行フェーズに入ってからは、カメラ操作とデバッグ遷移以外の入力を受け付けない
        /// </summary>
        protected override bool CanAcceptDefault()
        {
            if( _phase != Phase.SELECT_OPTION ) { return false; }
            return base.CanAcceptDefault();
        }

        public int GetCurrentIndex() => _cmdIdxVal.index;

        protected override bool AcceptDirection( InputContext context )
        {
            return _commandList.OperateListCursor( context.Cursor );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            ExecuteSelectedOption( ( TILE_MENU_OPTION_TAG ) _cmdIdxVal.value );

            return true;
        }

        private void ExecuteSelectedOption( TILE_MENU_OPTION_TAG option )
        {
            switch( option )
            {
                case TILE_MENU_OPTION_TAG.OPTION:
                    _optionHandler.ScheduleRun();
                    _phase = Phase.END;
                    break;

                case TILE_MENU_OPTION_TAG.TURN_END:
                    IsTurnEndSelected = true;
                    _phase = Phase.END;
                    break;
            }
        }

        private List<TILE_MENU_OPTION_TAG> GetOptions()
        {
            return new List<TILE_MENU_OPTION_TAG>
            {
                TILE_MENU_OPTION_TAG.OPTION,
                TILE_MENU_OPTION_TAG.TURN_END,
            };
        }
    }
}
