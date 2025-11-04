using Froniter.StateMachine;
using Zenject;
using static Constants;

namespace Frontier.StateMachine
{
    public sealed class DeployPhaseHandler : PhaseHandlerBase
    {
        override public void Init()
        {
            base.Init();

            _uiSystem.DeployUi.Init();

            // TODO : 配置出来るタイル以外はそのことを示す表示にする処理

            // TODO : 選択グリッドの設定
        }

        override public void Update()
        {
            base.Update();
        }

        override public void Exit()
        {
            // TODO : 味方の配置内容を確定させる処理

            _uiSystem.DeployUi.Exit();

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
             *      PlacementSelectTile
             *              ｜
             *              ├─ CharacterStatusViewState
             *              ｜
             *              └─ PlacementConfirmCompleted
             *              
             */

            RootNode = _hierarchyBld.InstantiateWithDiContainer<PlacementSelectTile>( false );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlacementConfirmCompleted>( false ) );

            CurrentNode = RootNode;
        }
    }
}