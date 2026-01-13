using Frontier.StateMachine;

namespace Frontier.Recruitment
{
    public sealed class RecruitmentPhaseHandler : PhaseHandlerBase
    {
        private RecruitmentPhasePresenter _presenter = null;

        override public void Init()
        {
            base.Init();

            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<RecruitmentPhasePresenter>( true ) );

            _presenter.Init();

            AssignPresenterToNodes( RootNode, _presenter );
        }

        override public void Update()
        {
            base.Update();

            _presenter.Update();
        }

        override public void Exit()
        {
            _presenter.Exit();

            base.Exit();
        }

        /// <summary>
        /// 遷移の木構造を作成します
        /// </summary>
        override protected void CreateTree()
        {
            // 遷移木の作成
            // MEMO : 別のファイル(XMLなど)から読み込んで作成出来るようにするのもアリ

            /*
             *  親子図
             * 
             *      RecruitmentRootState
             *              ｜
             *              ├─ CharacterStatusViewState
             *              ｜
             *              └─ RecruitmentConfirmCompletedState
             *              
             */

            RootNode = _hierarchyBld.InstantiateWithDiContainer<RecruitmentRootState>( false );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<RecruitmentConfirmCompletedState>( false ) );

            CurrentNode = RootNode;
        }
    }
}