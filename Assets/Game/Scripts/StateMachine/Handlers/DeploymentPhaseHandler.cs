using Frontier.StateMachine;
using Frontier.Entities;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.StateMachine
{
    public sealed class DeploymentPhaseHandler : PhaseHandlerBase
    {
        private DeploymentPhasePresenter _presenter;

        public override void Init()
        {
            base.Init();

            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<DeploymentPhasePresenter>( true ) );

            _presenter.Init();

            AssignPresenterToNodes( RootNode, _presenter );

            // TODO : 配置出来るタイル以外はそのことを示す表示にする処理

            // TODO : 選択グリッドの設定
        }

        public override void Update()
        {
            base.Update();

            _presenter.Update();
        }

        public override void Exit()
        {
            _presenter.Exit();

            base.Exit();
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
             *      DeploymentRootState
             *              ｜
             *              ├─ CharacterStatusViewState
             *              ｜
             *              └─ DeploymentConfirmCompletedState
             *              
             */

            RootNode = _hierarchyBld.InstantiateWithDiContainer<DeploymentRootState>( false );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<DeploymentConfirmCompletedState>( false ) );

            CurrentNode = RootNode;
        }
    }
}