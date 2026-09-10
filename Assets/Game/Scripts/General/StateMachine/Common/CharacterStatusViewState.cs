using Frontier.Entities;
using Frontier.Presenter;
using Zenject;
using static Constants;

namespace Frontier.StateMachine
{
    /// <summary>
    /// キャラクターステータス表示状態
    /// </summary>
    public class CharacterStatusViewState : PhaseStateBase
    {
        // RecruitPhaseHandler(雇用・編成フェーズ)等、戦闘用カメラが存在しない文脈でも本ステートは使われるため Optional にする
        [Inject( Optional = true )] private BattleCameraController _btlCameraCtrl = null;

        private string[] _inputInfoStrings;
        private Character _targetChara = null;
        private StatusPresenter _statusPresenter = null;
        private InputCodeStringWrapper _inputInfoStrWrapper = null;

        public override void Init( object context )
        {
            base.Init( context );

            LazyInject.GetOrCreate( ref _statusPresenter, () => _hierarchyBld.InstantiateWithDiContainer<StatusPresenter>( false ) );
            _statusPresenter.Init();

            // INFOアイコンの文字列を設定
            _inputInfoStrings = new string[]
            {
                "SHOW\nTOOL TIP", // ツールチップ表示
                "HIDE\nTOOL TIP", // ツールチップ非表示
            };
            _inputInfoStrWrapper = new InputCodeStringWrapper( _inputInfoStrings[0] );

            ReceiveContext( ref _targetChara, context );
            NullCheck.AssertNotNull( _targetChara, nameof( _targetChara ) );
            _targetChara.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_CHARACTER_STATUS );
            _statusPresenter.OpenCharacterStatus( _targetChara );

            // ステータス画面表示中はステージ上のカメラ操作と競合させない
            _btlCameraCtrl?.SetInputEnabled( false );
        }

        public override bool Update()
        {
            // INFOアイコンの文字列を更新
            _inputInfoStrWrapper.Explanation = _statusPresenter.IsToolTipActive() ? _inputInfoStrings[1] : _inputInfoStrings[0];

            return ( 0 <= TransitIndex );
        }

        public override object ExitState()
        {
            _targetChara.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_CHARACTER );
            _statusPresenter.CloseCharacterStatus();
            _targetChara = null;

            // カメラ操作の受付を再開する
            _btlCameraCtrl?.SetInputEnabled( true );

            return base.ExitState();
        }

        public override void OnEnter( object context )
        {
            base.OnEnter( context);
        }

        public override void RestartState()
        {
            base.RestartState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
                ( GuideIcon.VERTICAL_CURSOR,    "SELECT",               CanAcceptDirection, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
                ( GuideIcon.CANCEL,             "BACK",                 CanAcceptDefault, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode ),
                ( GuideIcon.INFO,               _inputInfoStrWrapper,   CanAcceptDefault, new AcceptContextInput( AcceptInfo ),   0.0f, hashCode )
            );
        }

        /// <summary>
        /// ツールチップUIを表示しているときに限り、方向入力を受け付けます
        /// </summary>
        /// <returns></returns>
        protected override bool CanAcceptDirection()
        {
            return _statusPresenter.IsToolTipActive();
        }

        protected override bool AcceptDirection( InputContext context )
        {
            int addValue = 0;

            switch( context.Cursor )
            {
                case Direction.FORWARD:
                    addValue = -1;
                    break;
                case Direction.BACK:
                    addValue = 1;
                    break;
                default:
                    return false;
            }

            _statusPresenter.AddSelectCursorItemIndex( addValue );

            return true;
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// /// <returns>決定入力の有無</returns>
        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) return false;

            Back();

            return true;
        }

        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) return false;

            _statusPresenter.ToggleToolTipActive();

            return true;
        }
    }
}