using Frontier.Entities;
using Frontier.StateMachine;
using System.Linq;
using Zenject;

namespace Frontier.Battle
{
    public class PlayerPhaseHandler : TroopPhaseHandler
    {
        [Inject] private SkillActionReservationQueue _reservationQueue = null;

        private PlConfirmReservedActionsState _confirmReservedActionsState = null;

        [Inject]
        public PlayerPhaseHandler( HierarchyBuilderBase hierarchyBld ) : base( hierarchyBld )
        {
        }

        public override void Init()
        {
            base.Init();

            if( 0 < _btlRtnCtrl.BtlCharaCdr.GetCharacterCount( CHARACTER_TAG.PLAYER ) )
            {
                // 選択グリッドを(1番目の)プレイヤーのグリッド位置に合わせる
                Character player = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER ).First();
                _stgCtrl.ApplyGridCursor2CharacterTile( player );
                // ターン開始時の自軍キャラへの処理
                _btlRtnCtrl.BtlCharaCdr.ApplyTurnStartProccessingForGroup( CHARACTER_TAG.PLAYER );
            }

            AssignPresenterToNodes( RootNode, _presenter );
        }

        /// <summary>
        /// 更新を行います
        /// </summary>
        public override void Update()
        {
            base.Update();

            _presenter.Update();
        }

        /// <summary>
        /// 後更新を行います。
        /// フェーズ終了時にキューが残っていれば確認ステートへ遷移します。
        /// </summary>
        public override bool LateUpdate()
        {
            bool phaseEnded = base.LateUpdate();

            if( phaseEnded && !_reservationQueue.IsEmpty && _confirmReservedActionsState != null )
            {
                CurrentNode = _confirmReservedActionsState;
                CurrentNode.OnEnter( null );
                return false;
            }

            return phaseEnded;
        }

        public override void Exit()
        {
            // プレイヤー以外の攻撃範囲表示をすべてクリア
            foreach( var npc in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
            {
                npc.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesAllType();
            }

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
             *      PlPhaseAnimationState
             *              ｜
             *              ├─ PlSelectTileState
             *              ｜       ｜
             *              ｜       ├─ CharacterStatusViewState
             *              ｜       ｜
             *              ｜       ├─ PlConfrimTurnEnd
             *              ｜       ｜
             *              ｜       ├─ PlSelectReservedActionState (予約に対する操作選択、実行まで行う)
             *              ｜       ｜
             *              ｜       └─ PlSelectCommandState
             *              ｜                    ｜
             *              ｜                    ├─ PlWaitState
             *              ｜                    ｜
             *              ｜                    ├────────────────────────────────────────PlSelectSkillState
             *              ｜                    ｜                                                                                        ｜
             *              ｜                    ├───────────────────── PlAttackState                                └─ PlSkillActionToTargetState
             *              ｜                    ｜                                                ｜                                                     ｜
             *              ｜                    └─ PlMoveState                                  └─ CharacterStatusViewState              ├─ CharacterStatusViewState
             *              ｜                             ｜                                                                              ｜
             *              ｜                             ├─ CharacterStatusViewState                                                     └─ PlSkillUseOptionState
             *              ｜                             ｜
             *              ｜                             └─ PlAttackOnMoveState
             *              ｜
             *              └─ PlConfirmReservedActionsState  (index 1 : キュー実行確認)
             *
             */

            // MEMO : キャラクターステータス表示状態は、各所から遷移可能にするため、複数個所に配置しています。

            RootNode = _hierarchyBld.InstantiateWithDiContainer<PlPhaseStateAnimation>( false );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSelectTileState>( false ) );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmReservedActionsState>( false ) );
            _confirmReservedActionsState = RootNode.GetChildren<PlConfirmReservedActionsState>( 1 );
            // Children[0]はPlSelectTileState、Children[1]はPlConfirmReservedActionsState
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSelectCommandState>( false ) );
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmTurnEnd>( false ) );
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSelectReservedActionState>( false ) );
            // Children[0].Children[0]はPlSelectCommandState
            RootNode.Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlMoveState>( false ) );
            RootNode.Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlAttackState>( false ) );
            RootNode.Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSelectSkillState>( false ) );
            RootNode.Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlWaitState>( false ) );
            // Children[0].Children[0].Children[0]はPlMoveState。その子にPlAttackOnMoveStateを追加(※移動中に直接、攻撃へ遷移出来るように)
            RootNode.Children[0].Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlAttackOnMoveState>( false ) );
            RootNode.Children[0].Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            // Children[0].Children[0].Children[1]はPlAttackState。その子にCharacterStatusViewStateとPlConfirmKillReservedTargetStateを追加
            RootNode.Children[0].Children[0].Children[1].AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            RootNode.Children[0].Children[0].Children[1].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmKillReservedTargetState>( false ) );
            // Children[0].Children[0].Children[0].Children[0]はPlAttackOnMoveState。TransitTag.CONFIRM_KILL_RESERVED_TARGET(=1、PlAttackState側と共通)と
            // インデックスを揃えるため、CharacterStatusViewStateをChildren[0]として先に追加してからPlConfirmKillReservedTargetStateを追加する
            RootNode.Children[0].Children[0].Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            RootNode.Children[0].Children[0].Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmKillReservedTargetState>( false ) );
            // Children[0].Children[0].Children[2]はPlSelectSkillState。その子にPlSkillActionToTargetStateを追加
            RootNode.Children[0].Children[0].Children[2].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSkillActionToTargetState>( false ) );
            // Children[0].Children[0].Children[2].Children[0]はPlSkillActionToTargetState。その子にCharacterStatusViewState・PlSkillUseOptionState・PlConfirmKillReservedTargetStateを追加
            RootNode.Children[0].Children[0].Children[2].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false ) );
            RootNode.Children[0].Children[0].Children[2].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSkillUseOptionState>( false ) );
            RootNode.Children[0].Children[0].Children[2].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmKillReservedTargetState>( false ) );
        }
    }
}