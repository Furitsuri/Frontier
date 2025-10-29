﻿using Frontier.Battle;
using Frontier.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Frontier.StateMachine
{
    public class PlayerPhaseHandler : PhaseHandlerBase
    {
        /// <summary>
        /// 初期化を行います
        /// </summary>
        override public void Init()
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
        override public void Update()
        {
            base.Update();
        }

        override public void Exit()
        {
            // プレイヤー以外の攻撃範囲表示をすべてクリア
            foreach( var npc in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
            {
                npc.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshes();
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
             *                      ├─ PlConfrimTurnEnd
             *                      ｜
             *                      └─ PlSelectCommandState
             *                                   ├───────── PlMoveState
             *                                   ｜                       ｜
             *                                   ├─ PlAttackState       └─ PlAttackOnMoveState
             *                                   ｜
             *                                   └─ PlWaitState
             */

            RootNode = _hierarchyBld.InstantiateWithDiContainer<PlPhaseStateAnimation>( false );
            RootNode.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSelectTileState>( false ) );
            // Children[0]はPlSelectTileState
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSelectCommandState>( false ) );
            RootNode.Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmTurnEnd>( false ) );
            // Children[0].Children[0]はPlSelectCommandState
            RootNode.Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlMoveState>( false ) );
            RootNode.Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlAttackState>( false ) );
            RootNode.Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlWaitState>( false ) );
            // Children[0].Children[0].Children[0]はPlMoveState。その子にPlAttackOnMoveStateを追加(※移動中に直接、攻撃へ遷移出来るように)。
            RootNode.Children[0].Children[0].Children[0].AddChild( _hierarchyBld.InstantiateWithDiContainer<PlAttackOnMoveState>( false ) );

            CurrentNode = RootNode;
        }

        /// <summary>
        /// フェーズアニメーションを再生します
        /// </summary>
        override protected void StartPhaseAnim()
        {
            _btlUi.TogglePhaseUI( true, TurnType.PLAYER_TURN );
            _btlUi.StartAnimPhaseUI();
        }
    }
}