using Frontier.Entities;
using Frontier.Battle;
using System.Linq;
using UnityEngine;

namespace Frontier
{
    public class EnemyPhaseHandler : PhaseHandlerBase
    {
        /// <summary>
        /// 初期化を行います
        /// </summary>
        override public void Init()
        {
            // 目標座標や攻撃対象をリセット
            foreach (Enemy enemy in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.ENEMY))
            {
                enemy.GetAi().ResetDestinationAndTarget();
            }
            // MEMO : 上記リセット後に初期化する必要があるためにこの位置であることに注意
            base.Init();
            // 選択グリッドを(1番目の)敵のグリッド位置に合わせる
            if (0 < _btlRtnCtrl.BtlCharaCdr.GetCharacterCount(Character.CHARACTER_TAG.ENEMY) && _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.ENEMY) != null)
            {
                Character enemy = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.ENEMY).First();
                _stgCtrl.ApplyCurrentGrid2CharacterGrid(enemy);
            }
            // アクションゲージの回復
            _btlRtnCtrl.BtlCharaCdr.RecoveryActionGaugeForGroup(Character.CHARACTER_TAG.ENEMY);
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

        override protected void CreateTree()
        {
            // 遷移木の作成
            RootNode = _hierarchyBld.InstantiateWithDiContainer<EmSelectState>();
            RootNode.AddChild(_hierarchyBld.InstantiateWithDiContainer<EmMoveState>());
            RootNode.AddChild(_hierarchyBld.InstantiateWithDiContainer<EmAttackState>());
            RootNode.AddChild(_hierarchyBld.InstantiateWithDiContainer<EmWaitState>());

            CurrentNode = RootNode;
        }

        /// <summary>
        /// フェーズアニメーションを再生します
        /// </summary>
        override protected void StartPhaseAnim()
        {
            _btlUi.TogglePhaseUI(true, BattleRoutineController.TurnType.ENEMY_TURN);
            _btlUi.StartAnimPhaseUI();
        }
    }
}
