using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPhaseManager : PhaseManagerBase
{
    /// <summary>
    /// 初期化を行います
    /// </summary>
    override public void Init()
    {
        base.Init();

        // 選択グリッドを(1番目の)プレイヤーのグリッド位置に合わせる
        Player player = BattleManager.Instance.GetPlayerEnumerable().First();
        StageGrid.Instance.ApplyCurrentGrid2CharacterGrid(player);
        // アクションゲージの回復
        BattleManager.Instance.RecoveryActionGaugeForGroup( Character.CHARACTER_TAG.CHARACTER_PLAYER );
    }

    /// <summary>
    /// 更新を行います
    /// </summary>
    override public bool Update()
    {
        if( _isFirstUpdate )
        {
            // フェーズアニメーションの開始
            StartPhaseAnim();

            _isFirstUpdate = false;

            return false;
        }
        // フェーズアニメーション中は操作無効
        if( BattleUISystem.Instance.IsPlayingPhaseUI() )
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
        // TODO : 別のファイル(XMLなど)から読み込んで作成出来るようにするのもアリ

        RootNode = new PLSelectGrid();
        RootNode.AddChild(new PLSelectCommandState());
        RootNode.AddChild(new PLConfirmTurnEnd());
        RootNode.Children[0].AddChild(new PLMoveState());
        RootNode.Children[0].AddChild(new PLAttackState());
        RootNode.Children[0].AddChild(new PLWaitState());

        CurrentNode = RootNode;
    }

    /// <summary>
    /// フェーズアニメーションを再生します
    /// </summary>
    override protected void StartPhaseAnim()
    {
        BattleUISystem.Instance.TogglePhaseUI(true, BattleManager.TurnType.PLAYER_TURN);
        BattleUISystem.Instance.StartAnimPhaseUI();
    }
}
