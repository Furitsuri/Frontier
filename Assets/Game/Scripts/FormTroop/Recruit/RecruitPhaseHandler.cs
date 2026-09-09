using Frontier.StateMachine;
using Zenject;

namespace Frontier.FormTroop
{
    public class RecruitPhaseHandler : PhaseHandlerBase
    {
        private RecruitPhasePresenter _presenter = null;

        [Inject]
        public RecruitPhaseHandler( HierarchyBuilderBase hierarchyBld ) : base( hierarchyBld )
        {
            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<RecruitPhasePresenter>( true ) );
        }

        public override void Init()
        {
            base.Init();

            // LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<RecruitPhasePresenter>( true ) );

            _presenter.Init();

            AssignPresenterToNodes( RootNode, _presenter );
        }

        public override void Exit()
        {
            base.Exit();

            _presenter.Exit();
        }

        public override void Update()
        {
            base.Update();

            _presenter.Update();
        }

        /// <summary>
        /// 遷移の木構造を作成します
        /// </summary>
        protected override void CreateTree()
        {
            // 遷移木の作成
            // MEMO : 別のファイル(XMLなど)から読み込んで作成出来るようにするのもアリ

            /*
             *  親子図
             *
             *      RecruitTopMenuState (雇用/解雇 選択のクッション画面)
             *              ｜
             *              ├─ RecruitRootState (雇用候補選択画面。キャンセルでクッション画面へBack())
             *              ｜         ｜
             *              ｜         ├─ CharacterStatusViewState
             *              ｜         ｜
             *              ｜         └─ RecruitConfirmCompletedState
             *              ｜
             *              ├─ RecruitDismissState (自軍メンバー一覧・解雇画面。キャンセルでクッション画面へBack())
             *              ｜         ｜
             *              ｜         └─ RecruitDismissConfirmState
             *              ｜
             *              └─ RecruitTopMenuConfirmCancelState
             *
             */
            var employCandidateState = _hierarchyBld.InstantiateWithDiContainer<RecruitRootState>( false );
            employCandidateState.AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            employCandidateState.AddChild( _hierarchyBld.InstantiateWithDiContainer<RecruitConfirmCompletedState>( false ) );

            var dismissState = _hierarchyBld.InstantiateWithDiContainer<RecruitDismissState>( false );
            dismissState.AddChild( _hierarchyBld.InstantiateWithDiContainer<RecruitDismissConfirmState>( false ) );

            RootNode = _hierarchyBld.InstantiateWithDiContainer<RecruitTopMenuState>( false );
            RootNode.AddChild( employCandidateState );
            RootNode.AddChild( dismissState );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<RecruitTopMenuConfirmCancelState>( false ) );

            CurrentNode = RootNode;
        }
    }
}