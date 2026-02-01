using Frontier.StateMachine;

namespace Frontier.FormTroop
{
    public class RecruitPhaseHandler : PhaseHandlerBase
    {
        private RecruitPhasePresenter _presenter = null;

        public override void Init()
        {
            base.Init();

            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<RecruitPhasePresenter>( true ) );

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
             *      RecruitRootState
             *              ｜
             *              ├─ CharacterStatusViewState
             *              ｜
             *              └─ RecruitConfirmCompletedState
             *              
             */
            RootNode = _hierarchyBld.InstantiateWithDiContainer<RecruitRootState>( false );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<RecruitConfirmCompletedState>( false ) );

            CurrentNode = RootNode;
        }
    }
}