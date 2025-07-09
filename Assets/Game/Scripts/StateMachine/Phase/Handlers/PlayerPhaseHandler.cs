using Frontier.Battle;
using Frontier.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class PlayerPhaseHandler : PhaseHandlerBase
    {
        /// <summary>
        /// 初期化を行います
        /// </summary>
        override public void Init()
        {
            base.Init();

            if (0 < _btlRtnCtrl.BtlCharaCdr.GetCharacterCount(Character.CHARACTER_TAG.PLAYER))
            {
                // 選択グリッドを(1番目の)プレイヤーのグリッド位置に合わせる
                Character player = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.PLAYER).First();
                _stgCtrl.ApplyCurrentGrid2CharacterGrid(player);
                // アクションゲージの回復
                _btlRtnCtrl.BtlCharaCdr.RecoveryActionGaugeForGroup(Character.CHARACTER_TAG.PLAYER);
            }
        }

        /// <summary>
        /// 更新を行います
        /// </summary>
        override public bool Update()
        {
            if (_isFirstUpdate)
            {
                // フェーズアニメーションの開始
                StartPhaseAnim();

                _isFirstUpdate = false;

                return false;
            }
            // フェーズアニメーション中は操作無効
            if (_btlUi.IsPlayingPhaseUI())
            {
                return false;
            }

            return base.Update();
        }

        /// <summary>
        /// 遷移の木構造を作成します
        /// </summary>
        override protected void CreateTree()
        {
            // 遷移木の作成
            // MEMO : 別のファイル(XMLなど)から読み込んで作成出来るようにするのもアリ

            RootNode = _hierarchyBld.InstantiateWithDiContainer<PlSelectGridState>(false);
            RootNode.AddChild(_hierarchyBld.InstantiateWithDiContainer<PlSelectCommandState>(false));
            RootNode.AddChild(_hierarchyBld.InstantiateWithDiContainer<PlConfirmTurnEnd>(false));
            RootNode.Children[0].AddChild(_hierarchyBld.InstantiateWithDiContainer<PlMoveState>(false));
            RootNode.Children[0].AddChild(_hierarchyBld.InstantiateWithDiContainer<PlAttackState>(false));
            RootNode.Children[0].AddChild(_hierarchyBld.InstantiateWithDiContainer<PlWaitState>(false));

            CurrentNode = RootNode;
        }

        /// <summary>
        /// フェーズアニメーションを再生します
        /// </summary>
        override protected void StartPhaseAnim()
        {
            _btlUi.TogglePhaseUI(true, BattleRoutineController.TurnType.PLAYER_TURN);
            _btlUi.StartAnimPhaseUI();
        }
    }
}