using Frontier.StateMachine;
using Frontier.TroopEdit;
using Frontier.UI;
using Zenject;
using static Constants;

namespace Frontier.FormTroop
{
    /// <summary>
    /// TroopGridController(グリッド表示・カーソル移動・キャラクターのライフサイクル)を使う画面
    /// (RecruitEmployState/RecruitDismissState)の共通基底クラス。グリッド表示のライフサイクル・
    /// 方向入力・ステータス確認への遷移など、画面間で完全に一致する処理をここに集約する。
    /// Confirm/Cancel/Opt2の実処理(アニマ増減・雇用/解雇チェックのトグル方法・確定時にUserDomainへ
    /// 加える効果・ロスターの構築方法)は画面ごとに大きく異なるため、サブクラス側の責務として残す。
    /// </summary>
    public class RecruitGridStateBase : RecruitPhaseStateBase
    {
        /// <summary>
        /// このクラスを使う画面すべてに共通する子Stateの並び。RecruitPhaseHandler.CreateTree()での
        /// AddChild()呼び出し順と一致させること。
        /// </summary>
        protected enum GridTransitTag
        {
            CHARACTER_STATUS = 0,
            SUMMARY_CONFIRM,
            GREETING,
        }

        [Inject] protected UserDomain _userDomain = null;
        [Inject] protected GeneralHeaderPresenter _headerPresenter = null;

        protected TroopEditPresenter _troopEditPresenter      = null;
        protected CharacterParameterPresenter _paramPresenter = null;
        protected TroopGridController _gridController         = null;

        protected string[] _inputConfirmStrings;
        protected InputCodeStringWrapper _inputConfirmStrWrapper = null;

        /// <summary>
        /// SELECTアイコンの説明文言。画面固有の言い回しにしたい場合はオーバーライドしてください。
        /// </summary>
        protected virtual string SelectGuideText => "SELECT";

        /// <summary>
        /// Presenter/TroopGridControllerの生成とCONFIRMアイコン文言の初期化を行います。
        /// サブクラスのInit()冒頭(base.Init()の直後)で呼び出してください。
        /// </summary>
        protected void SetupGrid( LocKey title, string[] confirmStrings )
        {
            _inputConfirmStrings    = confirmStrings;
            _inputConfirmStrWrapper = new InputCodeStringWrapper( _inputConfirmStrings[0] );

            LazyInject.GetOrCreate( ref _troopEditPresenter, () => _hierarchyBld.InstantiateWithDiContainer<TroopEditPresenter>( false ) );
            _troopEditPresenter.Init();
            // RecruitTopMenuState(会話ウィンドウ+空の背景)から遷移しても背景の見た目が変わらないよう、
            // 部隊編集画面(FieldScene)専用の全画面背景はRecruitでは非表示にする
            _troopEditPresenter.SetBackgroundVisible( false );
            _headerPresenter.SetStateTitle( title );

            LazyInject.GetOrCreate( ref _paramPresenter, () => _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>(
                new object[] { _troopEditPresenter.CharacterParamUI, false }, false ) );
            _paramPresenter.Init();

            LazyInject.GetOrCreate( ref _gridController, () => _hierarchyBld.InstantiateWithDiContainer<TroopGridController>( false ) );
            _gridController.Init( _troopEditPresenter, _paramPresenter );
        }

        /// <summary>
        /// 表示対象が0件(isNoneAvailable)の場合のみ、店主の会話クッション画面へ遷移します
        /// (会話を閉じるとRestartState()でこの画面の表示に戻る)。
        /// </summary>
        protected void ShowNoneAvailableCushionIfNeeded( bool isNoneAvailable, LocKey message )
        {
            if( !isNoneAvailable ) { return; }

            _gridController.HideSelectionDisplay();
            SetSendTransitionContext( new TalkWindowCushionContext( LocKey.UI_TALK_SHOPKEEPER_NAME, message ) );
            TransitState( ( int ) GridTransitTag.GREETING );
        }

        /// <summary>
        /// 会話クッション画面等の子Stateから戻ってきた際に呼ばれます。
        /// 子State表示中に隠していたカーソル・選択中キャラクターのパラメータパネルを再表示します。
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();

            _gridController.ShowSelectionDisplay();
        }

        public override object ExitState()
        {
            _gridController.Close();
            _headerPresenter.ClearStateTitle();
            _headerPresenter.SetAnimaDiff( 0 );

            return base.ExitState();
        }

        /// <summary>
        /// 選択中キャラクターの有無に応じてCONFIRMアイコン文言を切り替えます。
        /// トグル状態の判定は画面ごとにデータソースが異なるためIsSelectedToggled()に委譲します。
        /// </summary>
        public override bool Update()
        {
            if( _gridController.SpawnedCharacters.Count > 0 )
            {
                _inputConfirmStrWrapper.Explanation = IsSelectedToggled() ? _inputConfirmStrings[1] : _inputConfirmStrings[0];
            }

            _headerPresenter.SetAnimaDiff( GetPendingAnimaDiff() );

            return ( 0 <= TransitIndex );
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR, SelectGuideText,         CanAcceptDefault,  new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,    _inputConfirmStrWrapper, CanAcceptConfirm,  new AcceptContextInput( AcceptConfirm ),    0.0f, hashCode),
               (GuideIcon.CANCEL,     "BACK",                  CanAcceptDefault,  new AcceptContextInput( AcceptCancel ),    0.0f, hashCode),
               (GuideIcon.INFO,       "STATUS",                CanAcceptInfo,     new AcceptContextInput( AcceptInfo ),      0.0f, hashCode),
               (GuideIcon.OPT2,       "COMPLETE",              CanAcceptOptional, new AcceptContextInput( AcceptOpt2 ),      0.0f, hashCode)
            );
        }

        /// <summary>
        /// 選択中キャラクターが存在しない場合はステータス表示への遷移を受け付けない。
        /// 選択中キャラクターの有無はTroopGridControllerに委譲する。
        /// </summary>
        protected override bool CanAcceptInfo()
        {
            return _gridController.SelectedCharacter != null;
        }

        protected override bool AcceptDirection( InputContext context )
        {
            return _gridController.MoveSelection( context.Cursor );
        }

        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            // ステータス表示ステートに、TroopGridControllerが管理する選択中キャラクターを渡す
            SetSendTransitionContext( _gridController.SelectedCharacter );
            // キャラクターステータス表示ステートへ遷移
            TransitState( ( int ) GridTransitTag.CHARACTER_STATUS );

            return true;
        }

        /// <summary>
        /// トグル済み人数を確認画面へ渡し、確定確認ステートへ遷移します。
        /// 人数の数え方(何をもって「チェック済み」とするか)は画面ごとにデータソースが異なるため
        /// GetToggledCount()に委譲します。
        /// </summary>
        protected override bool AcceptOpt2( InputContext context )
        {
            if( !base.AcceptOpt2( context ) ) { return false; }

            SetSendTransitionContext( GetToggledCount() );
            TransitState( ( int ) GridTransitTag.SUMMARY_CONFIRM );

            return true;
        }

        /// <summary>
        /// 選択中キャラクターがトグル済み(雇用チェック済み/解雇チェック済み)かどうかを返します。
        /// </summary>
        protected virtual bool IsSelectedToggled() { return false; }

        /// <summary>
        /// トグル済み(雇用/解雇チェック済み)の人数を返します。
        /// </summary>
        protected virtual int GetToggledCount() { return 0; }

        /// <summary>
        /// 現在の予備登録(雇用/解雇チェック)によって生じる所持アニマの増減差分を返します。
        /// ヘッダーのアニマ数値のすぐ下に表示するために、Update()から毎フレーム参照されます。
        /// </summary>
        protected virtual int GetPendingAnimaDiff() { return 0; }
    }
}
