using Frontier.Entities;
using Frontier.Battle;
using System.Linq;
using UnityEngine;

namespace Frontier.StateMachine
{
    public class EnemyPhaseHandler : PhaseHandlerBase
    {
        /// <summary>
        /// 初期化を行います
        /// </summary>
        override public void Init()
        {
            // 目標座標や攻撃対象をリセット
            foreach( Enemy enemy in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY ) )
            {
                enemy.GetAi().ResetDestinationAndTarget();
            }
            // MEMO : 上記リセット後に初期化する必要があるためにこの位置であることに注意
            base.Init();
            // 選択グリッドを(1番目の)敵のグリッド位置に合わせる
            if( 0 < _btlRtnCtrl.BtlCharaCdr.GetCharacterCount( CHARACTER_TAG.ENEMY ) && _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY ) != null )
            {
                Character enemy = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY ).First();
                _stgCtrl.ApplyCurrentGrid2CharacterTile( enemy );
            }
            // アクションゲージの回復
            _btlRtnCtrl.BtlCharaCdr.RecoveryActionGaugeForGroup( CHARACTER_TAG.ENEMY );
        }

        /// <summary>
        /// 更新を行います
        /// </summary>
        override public void Update()
        {
            base.Update();
        }

        override protected void CreateTree()
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
             *                      └─ EmWaitState
             */

            RootNode = _hierarchyBld.InstantiateWithDiContainer<EmPhaseStateAnimation>( false );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<EmSelectState>( false ) );
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<EmMoveState>( false ) );
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<EmAttackState>( false ) );
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<EmWaitState>( false ) );

            CurrentNode = RootNode;
        }
    }
}