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
             *              ├─ RecruitEmployState (雇用候補選択画面。キャンセルでクッション画面へBack())
             *              ｜         ｜
             *              ｜         ├─ CharacterStatusViewState
             *              ｜         ｜
             *              ｜         ├─ RecruitConfirmCompletedState (絞り込み後の一覧を十字キーで選択可能)
             *              ｜         ｜         ｜
             *              ｜         ｜         └─ CharacterStatusViewState (確認画面上でのINFO入力用)
             *              ｜         ｜
             *              ｜         └─ TalkWindowCushionState (雇用可能な候補が0体の場合のみ経由する店主の会話クッション。汎用State)
             *              ｜
             *              ├─ RecruitDismissState (自軍メンバー一覧・解雇画面。キャンセルでクッション画面へBack())
             *              ｜         ｜
             *              ｜         ├─ CharacterStatusViewState
             *              ｜         ｜
             *              ｜         ├─ RecruitDismissConfirmCompletedState (絞り込み後の一覧を十字キーで選択可能)
             *              ｜         ｜         ｜
             *              ｜         ｜         └─ CharacterStatusViewState (確認画面上でのINFO入力用)
             *              ｜         ｜
             *              ｜         └─ TalkWindowCushionState (解雇可能なメンバーが0体の場合のみ経由する店主の会話クッション。汎用State)
             *              ｜
             *              └─ RecruitTopMenuConfirmCancelState
             *
             */
            var employConfirmState = _hierarchyBld.InstantiateWithDiContainer<RecruitConfirmCompletedState>( false );
            employConfirmState.AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );

            var employState = _hierarchyBld.InstantiateWithDiContainer<RecruitEmployState>( false );
            employState.AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            employState.AddChild( employConfirmState );
            employState.AddChild( _hierarchyBld.InstantiateWithDiContainer<TalkWindowCushionState>( false ) );

            var dismissConfirmState = _hierarchyBld.InstantiateWithDiContainer<RecruitDismissConfirmCompletedState>( false );
            dismissConfirmState.AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );

            var dismissState = _hierarchyBld.InstantiateWithDiContainer<RecruitDismissState>( false );
            dismissState.AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            dismissState.AddChild( dismissConfirmState );
            dismissState.AddChild( _hierarchyBld.InstantiateWithDiContainer<TalkWindowCushionState>( false ) );

            RootNode = _hierarchyBld.InstantiateWithDiContainer<RecruitTopMenuState>( false );
            RootNode.AddChild( employState );
            RootNode.AddChild( dismissState );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<RecruitTopMenuConfirmCancelState>( false ) );

            CurrentNode = RootNode;
        }
    }
}