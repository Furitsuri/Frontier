using Frontier.Entities;
using Frontier.StateMachine;
using System.Linq;

namespace Frontier.Battle
{
    public class PlayerPhaseHandler : TroopPhaseHandler
    {
        public override void Init()
        {
            base.Init();

            if( 0 < _btlRtnCtrl.BtlCharaCdr.GetCharacterCount( CHARACTER_TAG.PLAYER ) )
            {
                // 選択グリッドを(1番目の)プレイヤーのグリッド位置に合わせる
                Character player = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER ).First();
                _stgCtrl.ApplyCurrentGrid2CharacterTile( player );
                // アクションゲージの回復
                _btlRtnCtrl.BtlCharaCdr.RecoveryActionGaugeForGroup( CHARACTER_TAG.PLAYER );
            }
        }

        /// <summary>
        /// 更新を行います
        /// </summary>
        public override void Update()
        {
            base.Update();

            _presenter.Update();
        }

        public override void Exit()
        {
            // プレイヤー以外の攻撃範囲表示をすべてクリア
            foreach( var npc in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
            {
                npc.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshes();
            }

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
             *      PlPhaseAnimationState
             *              ｜
             *              └─ PlSelectTileState
             *                      ｜
             *                      ├─ CharacterStatusViewState
             *                      ｜
             *                      ├─ PlConfrimTurnEnd
             *                      ｜
             *                      └─ PlSelectCommandState
             *                                   ｜
             *                                   ├─ PlWaitState
             *                                   ｜                       
             *                                   ├───────────────────── PlAttackState
             *                                   ｜                                                ｜
             *                                   └─ PlMoveState                                  └─ CharacterStatusViewState
             *                                            ｜
             *                                            ├─ CharacterStatusViewState
             *                                            ｜
             *                                            └─ PlAttackOnMoveState
             *                                   
             */

            // MEMO : キャラクターステータス表示状態は、各所から遷移可能にするため、複数個所に配置しています。

            RootNode = _hierarchyBld.InstantiateWithDiContainer<PlPhaseStateAnimation>( false );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSelectTileState>( false ) );
            // Children[0]はPlSelectTileState
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSelectCommandState>( false ) );
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmTurnEnd>( false ) );
            // Children[0].Children[0]はPlSelectCommandState
            RootNode.Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlMoveState>( false ) );
            RootNode.Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlAttackState>( false ) );
            RootNode.Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlWaitState>( false ) );
            // Children[0].Children[0].Children[0]はPlMoveState。その子にPlAttackOnMoveStateを追加(※移動中に直接、攻撃へ遷移出来るように)
            RootNode.Children[0].Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlAttackOnMoveState>( false ) );
            RootNode.Children[0].Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            // Children[0].Children[0].Children[1]はPlAttackState。その子にCharacterStatusViewStateを追加
            RootNode.Children[0].Children[0].Children[1].AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );

            CurrentNode = RootNode;
        }
    }
}