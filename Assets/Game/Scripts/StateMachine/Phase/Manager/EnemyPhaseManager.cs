using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontier
{
    public class EnemyPhaseManager : PhaseManagerBase
    {
        /// <summary>
        /// 初期化を行います
        /// </summary>
        override public void Init()
        {
            // 目標座標や攻撃対象をリセット
            foreach (Enemy enemy in _btlMgr.GetEnemyEnumerable())
            {
                enemy.EmAI.ResetDestinationAndTarget();
            }
            // MEMO : 上記リセット後に初期化する必要があるためにこの位置であることに注意
            base.Init();
            // 選択グリッドを(1番目の)敵のグリッド位置に合わせる
            if (0 < _btlMgr.GetCharacterCount(Character.CHARACTER_TAG.ENEMY) && _btlMgr.GetEnemyEnumerable() != null)
            {
                Enemy enemy = _btlMgr.GetEnemyEnumerable().First();
                _stageCtrl.ApplyCurrentGrid2CharacterGrid(enemy);
            }
            // アクションゲージの回復
            _btlMgr.RecoveryActionGaugeForGroup(Character.CHARACTER_TAG.ENEMY);
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
            if (BattleUISystem.Instance.IsPlayingPhaseUI())
            {
                return false;
            }

            return base.Update();
        }

        override protected void CreateTree()
        {
            // 遷移木の作成
            RootNode = new EMSelectState();
            RootNode.AddChild(new EMMoveState());
            RootNode.AddChild(new EMAttackState());
            RootNode.AddChild(new EMWaitState());

            CurrentNode = RootNode;
        }

        /// <summary>
        /// フェーズアニメーションを再生します
        /// </summary>
        override protected void StartPhaseAnim()
        {
            BattleUISystem.Instance.TogglePhaseUI(true, BattleManager.TurnType.ENEMY_TURN);
            BattleUISystem.Instance.StartAnimPhaseUI();
        }
    }
}
