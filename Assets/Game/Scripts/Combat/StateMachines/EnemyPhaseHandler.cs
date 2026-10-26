using Frontier.Entities;
using Frontier.StateMachine;
using System.Linq;
using Zenject;

namespace Frontier.Battle
{
    public class EnemyPhaseHandler : TroopPhaseHandler
    {
        [Inject]
        public EnemyPhaseHandler( HierarchyBuilderBase hierarchyBld ) : base( hierarchyBld )
        {
        }

        public override void Init()
        {
            // 目標座標や攻撃対象をリセット
            foreach( Enemy enemy in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY ) )
            {
                enemy.BattleLogic.GetAi().ResetDestinationAndTarget();
            }
            // 待ち構えている(INTERCEPT型の)敵のうち、行動範囲に攻撃対象が入ってきた集団に行動を開始させる
            // MEMO : 各敵の手番毎に判定すると、先に手番を終えた構成員がそのターン行動出来なくなるため、フェーズ開始時にまとめて判定する
            _btlRtnCtrl.BtlCharaCdr.InterceptGroupReg.ActivateGroupsDetectingOpponent( CHARACTER_TAG.ENEMY );
            // MEMO : 上記リセット後に初期化する必要があるためにこの位置であることに注意
            base.Init();
            // 選択グリッドを(1番目の)敵のグリッド位置に合わせる
            if( 0 < _btlRtnCtrl.BtlCharaCdr.GetCharacterCount( CHARACTER_TAG.ENEMY ) && _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY ) != null )
            {
                Character enemy = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY ).First();
                _stgCtrl.ApplyGridCursor2CharacterTile( enemy );
            }
            // ターン開始時の自軍キャラへの処理
            _btlRtnCtrl.BtlCharaCdr.ApplyTurnStartProccessingForGroup( CHARACTER_TAG.ENEMY );

            AssignPresenterToNodes( RootNode, _presenter );
        }

        /// <summary>
        /// 更新を行います
        /// </summary>
        protected override void CreateTree()
        {
            // 遷移木の作成
            /*
             *  親子図
             * 
             *      EmPhaseAnimationState
             *              ｜
             *              └─ EmSelectState
             *                      ｜
             *                      ├─ EmMoveState
             *                      ｜
             *                      ├─ EmAttackState
             *                      ｜
             *                      ├─ EmSkillState
             *                      ｜
             *                      └─ EmWaitState
             */

            RootNode = _hierarchyBld.InstantiateWithDiContainer<EmPhaseStateAnimation>( false );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<EmSelectState>( false ) );
            RootNode.GetChildren<PhaseStateBase>(0).AddChild( _hierarchyBld.InstantiateWithDiContainer<EmMoveState>( false ) );
            RootNode.GetChildren<PhaseStateBase>(0).AddChild( _hierarchyBld.InstantiateWithDiContainer<EmAttackState>( false ) );
            RootNode.GetChildren<PhaseStateBase>(0).AddChild( _hierarchyBld.InstantiateWithDiContainer<EmSkillState>( false ) );
            RootNode.GetChildren<PhaseStateBase>(0).AddChild( _hierarchyBld.InstantiateWithDiContainer<EmWaitState>( false ) );
        }
    }
}