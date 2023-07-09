using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyPhaseManager : PhaseManagerBase
{
    /// <summary>
    /// 初期化を行います
    /// </summary>
    override public void Init()
    {
        // 目標座標や攻撃対象をリセット
        foreach( Enemy enemy in BattleManager.Instance.GetEnemyEnumerable() )
        {
            enemy.EmAI.ResetDestinationAndTarget();
        }

        // 選択グリッドを(1番目の)敵のグリッド位置に合わせる
        if (BattleManager.Instance.GetEnemyEnumerable() != null)
        {
            Enemy enemy = BattleManager.Instance.GetEnemyEnumerable().First();
            StageGrid.Instance.ApplyCurrentGrid2CharacterGrid(enemy);
        }

        base.Init();
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
