using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorHandler : EditorHandlerBase
    {
        private Action<int, int> PlaceTileCallback;
        private Func<string, bool> LoadStageCallback;

        private StageEditorPresenter _stageEditorView   = null;

        public void Init(StageEditorPresenter stageEditorView, Action<int, int> placeTileCallback, Func<string, bool> loadStageCallback)
        {
            _stageEditorView    = stageEditorView;
            PlaceTileCallback   = placeTileCallback;
            LoadStageCallback   = loadStageCallback;

            base.Init();
        }

        override protected void CreateTree()
        {
            StageEditorEditingState stageEditorEditingState = _hierarchyBld.InstantiateWithDiContainer<StageEditorEditingState>(false);
            stageEditorEditingState.SetCallbacks(PlaceTileCallback, LoadStageCallback);

            StageEditorSaveState stageEditorSaveState = _hierarchyBld.InstantiateWithDiContainer<StageEditorSaveState>(false);
            stageEditorSaveState.SetCallbacks( _stageEditorView.ToggleNotifyView, _stageEditorView.SetNotifyWord );

            StageEditorLoadState stageEditorLoadState = _hierarchyBld.InstantiateWithDiContainer<StageEditorLoadState>(false);
            stageEditorLoadState.SetCallbacks(_stageEditorView.ToggleNotifyView, _stageEditorView.SetNotifyWord);

            // 遷移木の作成
            RootNode = stageEditorEditingState;
            RootNode.AddChild(stageEditorSaveState);
            RootNode.AddChild(stageEditorLoadState);
            CurrentNode = RootNode;
        }
    }
}