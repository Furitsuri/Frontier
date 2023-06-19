using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPhaseManager : PhaseManagerBase
{
    /// <summary>
    /// 初期化を行います
    /// </summary>
    override public void Init()
    {
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

    override protected void CreateStateTree()
    {
        // 遷移木の作成
        // TODO : 別のファイル(XMLなど)から読み込んで作成出来ると便利

        m_RootState = new EMMoveState();

        m_CurrentState = m_RootState;
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
