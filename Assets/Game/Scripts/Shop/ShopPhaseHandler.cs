using Frontier.StateMachine;
using Frontier.UI;
using Zenject;
using static Constants;

namespace Frontier.Shop
{
    /// <summary>
    /// ショップのステート木(商品一覧→退店確認→挨拶)を実行するHandlerです。
    /// ShopPresenterを生成・保持し、各Stateへ配布します(RecruitPhaseHandlerと同じ構成)。
    /// 画面上部の全幅ヘッダー(画面タイトル・部隊人数・所持アニマ・購入予定によるアニマ増減差分)は、
    /// ショップ全体を通して常時表示するため、特定のStateではなくフェーズ全体のライフサイクルを持つ
    /// このHandlerで表示/更新/非表示を行います。
    /// 品揃え・在庫・購入処理といった実データはShopHandler側にあり、このクラスは持ちません。
    /// </summary>
    public class ShopPhaseHandler : PhaseHandlerBase
    {
        private ShopPresenter _presenter                = null;
        private GeneralHeaderPresenter _headerPresenter = null;
        private UserDomain _userDomain                  = null;

        [Inject]
        public ShopPhaseHandler( HierarchyBuilderBase hierarchyBld, GeneralHeaderPresenter headerPresenter, UserDomain userDomain ) : base( hierarchyBld )
        {
            _headerPresenter = headerPresenter;
            _userDomain      = userDomain;

            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<ShopPresenter>( false ) );
        }

        public override void Init()
        {
            base.Init();

            AssignPresenterToNodes( RootNode, _presenter );

            _presenter.Open();

            _headerPresenter.Show();
            _headerPresenter.SetStateTitle( LocKey.UI_FACILITY_SHOP );
            RefreshHeaderInfo();
        }

        public override void Update()
        {
            base.Update();

            // どの子Stateがアクティブでも、購入による所持アニマの増減を即時に反映できるよう、
            // Handler側のUpdate()で毎フレーム更新する
            RefreshHeaderInfo();
        }

        public override bool LateUpdate()
        {
            bool isFinished = base.LateUpdate();

            if( isFinished ) { CloseUi(); }

            return isFinished;
        }

        public override void Exit()
        {
            base.Exit();

            CloseUi();
        }

        /// <summary>
        /// 遷移の木構造を作成します
        /// </summary>
        protected override void CreateTree()
        {
            /*
             *  親子図
             *
             *      ShopBrowseState (商品一覧。上下でカーソル移動、決定で購入。複数個買える場合は個数選択を挟む)
             *              ｜
             *              ├─ ShopLeaveConfirmState (退店確認。「はい」でショップ全体を終了)
             *              ｜         ｜
             *              ｜         └─ TalkWindowCushionState (退店時の店主の挨拶。汎用State)
             *              ｜
             *              └─ ShopQuantityState (購入個数の選択。決定で購入して商品一覧へ戻る)
             *
             *  ※ShopBrowseState.ShopBrowseTransitTagの並び(LEAVE_CONFIRM, QUANTITY)は、下記AddChildの順序と一致させること
             */
            var farewellState = _hierarchyBld.InstantiateWithDiContainer<TalkWindowCushionState>( false );

            var leaveConfirmState = _hierarchyBld.InstantiateWithDiContainer<ShopLeaveConfirmState>( false );
            leaveConfirmState.AddChild( farewellState );

            var quantityState = _hierarchyBld.InstantiateWithDiContainer<ShopQuantityState>( false );

            RootNode = _hierarchyBld.InstantiateWithDiContainer<ShopBrowseState>( false );
            RootNode.AddChild( leaveConfirmState );
            RootNode.AddChild( quantityState );

            CurrentNode = RootNode;
        }

        private void RefreshHeaderInfo()
        {
            _headerPresenter.SetHeaderInfo( _userDomain.Anima, _userDomain.Members.Count, TROOP_MAX_MEMBERS );

            // 個数選択中は、購入予定額を所持アニマの下に増減差分として表示する(雇用/解雇画面と同じ表示形式)
            _headerPresenter.SetAnimaDiff( _presenter.PendingAnimaDiff );
        }

        private void CloseUi()
        {
            _presenter.Close();

            _headerPresenter.SetAnimaDiff( 0 );
            _headerPresenter.ClearStateTitle();
            _headerPresenter.Hide();
        }
    }
}
