using Frontier.UI;
using Frontier.StateMachine;
using Zenject;
using static Constants;

namespace Frontier.FormTroop
{
    public class RecruitPhaseHandler : PhaseHandlerBase
    {
        private RecruitPhasePresenter _presenter = null;
        private GeneralHeaderPresenter _headerPresenter = null;
        private UserDomain _userDomain = null;

        [Inject]
        public RecruitPhaseHandler( HierarchyBuilderBase hierarchyBld, GeneralHeaderPresenter headerPresenter, UserDomain userDomain ) : base( hierarchyBld )
        {
            _headerPresenter = headerPresenter;
            _userDomain      = userDomain;

            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<RecruitPhasePresenter>( true ) );
        }

        public override void Init()
        {
            base.Init();

            // LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<RecruitPhasePresenter>( true ) );

            _presenter.Init();

            AssignPresenterToNodes( RootNode, _presenter );

            // 画面上部の全幅HUD(所持アニマ・部隊人数)はRecruitフェーズ全体を通して常時表示するため、
            // 特定のStateではなくフェーズ全体のライフサイクルを持つこのHandlerで表示/更新/非表示を行う
            _headerPresenter.Show();
        }

        public override void Exit()
        {
            base.Exit();

            _presenter.Exit();

            _headerPresenter.Hide();
        }

        public override void Update()
        {
            base.Update();

            // どの子State(TopMenu/雇用/解雇)がアクティブでも所持アニマ・部隊人数の変化を
            // 反映できるよう、Handler側のUpdate()で毎フレーム更新する
            _headerPresenter.SetHeaderInfo( _userDomain.Anima, _userDomain.Members.Count, TROOP_MAX_MEMBERS );
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