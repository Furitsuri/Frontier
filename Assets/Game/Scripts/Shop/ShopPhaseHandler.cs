using Frontier.StateMachine;
using Zenject;

namespace Frontier.Shop
{
    /// <summary>
    /// ショップのステート木(商品一覧→退店確認→挨拶)を実行するHandlerです。
    /// ShopPresenterを生成・保持し、各Stateへ配布します(RecruitPhaseHandlerと同じ構成)。
    /// 品揃え・在庫・購入処理といった実データはShopHandler側にあり、このクラスは持ちません。
    /// </summary>
    public class ShopPhaseHandler : PhaseHandlerBase
    {
        private ShopPresenter _presenter = null;

        [Inject]
        public ShopPhaseHandler( HierarchyBuilderBase hierarchyBld ) : base( hierarchyBld )
        {
            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<ShopPresenter>( false ) );
        }

        public override void Init()
        {
            base.Init();

            AssignPresenterToNodes( RootNode, _presenter );

            _presenter.Open();
        }

        public override bool LateUpdate()
        {
            bool isFinished = base.LateUpdate();

            if( isFinished ) { _presenter.Close(); }

            return isFinished;
        }

        public override void Exit()
        {
            base.Exit();

            _presenter.Close();
        }

        /// <summary>
        /// 遷移の木構造を作成します
        /// </summary>
        protected override void CreateTree()
        {
            /*
             *  親子図
             *
             *      ShopBrowseState (商品一覧。上下でカーソル移動、決定で購入)
             *              ｜
             *              └─ ShopLeaveConfirmState (退店確認。「はい」でショップ全体を終了)
             *                        ｜
             *                        └─ TalkWindowCushionState (退店時の店主の挨拶。汎用State)
             */
            var farewellState = _hierarchyBld.InstantiateWithDiContainer<TalkWindowCushionState>( false );

            var leaveConfirmState = _hierarchyBld.InstantiateWithDiContainer<ShopLeaveConfirmState>( false );
            leaveConfirmState.AddChild( farewellState );

            RootNode = _hierarchyBld.InstantiateWithDiContainer<ShopBrowseState>( false );
            RootNode.AddChild( leaveConfirmState );

            CurrentNode = RootNode;
        }
    }
}
