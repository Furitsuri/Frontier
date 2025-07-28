using Frontier.Battle;
using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class EditorHandlerBase : Tree<EditorStateBase>
{
    protected bool _isInitReserved = false;
    protected HierarchyBuilderBase _hierarchyBld = null;

    [Inject]
    public void Construct(HierarchyBuilderBase hierarchyBld)
    {
        _hierarchyBld = hierarchyBld;
    }

    virtual public void Init()
    {
        // 遷移木の作成
        CreateTree();

        CurrentNode.Init();
    }

    virtual public bool Update()
    {
        if (_isInitReserved)
        {
            CurrentNode.RunState();
            _isInitReserved = false;
        }

        // 現在実行中のステートを更新
        if (CurrentNode.Update())
        {
            if (CurrentNode.IsBack() && CurrentNode.Parent == null)
            {
                CurrentNode.ExitState();

                return true;
            }
        }

        return false;
    }

    virtual public void LateUpdate()
    {
        // ステートの遷移を監視
        int transitIndex = CurrentNode.TransitIndex;
        if (0 <= transitIndex)
        {
            CurrentNode.ExitState();
            CurrentNode = CurrentNode.GetChildren<EditorStateBase>(transitIndex);
            _isInitReserved = true;
        }
        else if (CurrentNode.IsBack())
        {
            CurrentNode.ExitState();
            CurrentNode = CurrentNode.GetParent<EditorStateBase>();
            _isInitReserved = true;
        }
    }

    virtual public void Run()
    {
        // ステートの実行
        CurrentNode.RunState();
    }

    virtual public void Restart()
    {
        // ステートの再開
        CurrentNode.RestartState();
    }

    virtual public void Pause()
    {
        // ステートの一時停止
        CurrentNode.PauseState();
    }

    virtual public void Exit()
    {
        // ステートの終了
        CurrentNode.ExitState();
    }

    /// <summary>
    /// フェーズアニメーションを再生します
    /// </summary>
    virtual protected void StartPhaseAnim()
    {
    }
}