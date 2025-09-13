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
        private Func< int, StageEditMode > ChangeEditModeCallback;

        private StageEditorPresenter _stageEditorView   = null;

        public void Init(StageEditorPresenter stageEditorView, Action<int, int> placeTileCb, Func<string, bool> loadStageCb, Func<int, StageEditMode> changeEditModeCb )
        {
            _stageEditorView        = stageEditorView;
            PlaceTileCallback       = placeTileCb;
            LoadStageCallback       = loadStageCb;
            ChangeEditModeCallback = changeEditModeCb;

            base.Init();
        }

        override protected void CreateTree()
        {
            StageEditorEditingState stageEditorEditingState = _hierarchyBld.InstantiateWithDiContainer<StageEditorEditingState>(false);
            stageEditorEditingState.SetCallbacks(PlaceTileCallback, LoadStageCallback, ChangeEditModeCallback);

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