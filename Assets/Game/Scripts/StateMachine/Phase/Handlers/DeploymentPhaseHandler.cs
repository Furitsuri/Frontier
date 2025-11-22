using Froniter.StateMachine;
using Frontier.Entities;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.StateMachine
{
    public sealed class DeploymentPhaseHandler : PhaseHandlerBase
    {
        private DeploymentPhasePresenter _presenter;

        override public void Init()
        {
            base.Init();

            _presenter = _hierarchyBld.InstantiateWithDiContainer<DeploymentPhasePresenter>( true );
            NullCheck.AssertNotNull( _presenter, "_presenter" );
            _presenter.Init();

            var drs = RootNode as DeploymentRootState;
            if( drs != null )
            {
                AssignPresenterToNodes( drs, _presenter );
            }

            // TODO : 配置出来るタイル以外はそのことを示す表示にする処理

            // TODO : 選択グリッドの設定
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

        private void AssignPresenterToNodes( DeploymentPhaseStateBase targetNode, DeploymentPhasePresenter presenter )
        {
            if( null == targetNode ) { return; }

            targetNode.AssignPresenter( presenter );

            foreach( var childNode in targetNode.GetChildNodeEnumerable<DeploymentPhaseStateBase>() )
            {
                AssignPresenterToNodes( childNode as DeploymentPhaseStateBase, presenter );
            }
        }
    }
}