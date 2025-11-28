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
        private Action<int, int> ResizeTileGridCallback;
        private Action<int, int> ToggleDeployableCallback;
        private Func<string, bool> SaveStageCallback;
        private Func<string, bool> LoadStageCallback;
        private Func< int, StageEditMode > ChangeEditModeCallback;

        private StageEditorPresenter _stageEditorView   = null;

        public void Init( StageEditorPresenter stageEditorView, Action<int, int> placeTileCb, Action<int, int> risizeTileGridCb, Action<int, int> toggleDeployableCb, Func<string, bool> saveStageCb, Func<string, bool> loadStageCb, Func<int, StageEditMode> changeEditModeCb )
        {
            _stageEditorView            = stageEditorView;
            PlaceTileCallback           = placeTileCb;
            ResizeTileGridCallback      = risizeTileGridCb;
            ToggleDeployableCallback    = toggleDeployableCb;
            SaveStageCallback           = saveStageCb;
            LoadStageCallback           = loadStageCb;
            ChangeEditModeCallback      = changeEditModeCb;

            base.Init();
        }

        override protected void CreateTree()
        {
            /*
             *  親子図
             * 
             *      StageEditorEditingState
             *                ｜
             *                ├─ StageEditorSaveState
             *                ｜
             *                └─ StageEditorLoadState
             *                                   
             */

            var stageEditorEditingState = _hierarchyBld.InstantiateWithDiContainer<StageEditorEditingState>(false);
            stageEditorEditingState.SetCallbacks(PlaceTileCallback, ResizeTileGridCallback, ToggleDeployableCallback, ChangeEditModeCallback);

            var stageEditorSaveState = _hierarchyBld.InstantiateWithDiContainer<StageEditorSaveState>(false);
            stageEditorSaveState.SetCallbacks( SaveStageCallback, _stageEditorView.SetMessageWord );

            var stageEditorLoadState = _hierarchyBld.InstantiateWithDiContainer<StageEditorLoadState>(false);
            stageEditorLoadState.SetCallbacks( LoadStageCallback, _stageEditorView.SetMessageWord);

            // 遷移木の作成
            RootNode = stageEditorEditingState;
            RootNode.AddChild(stageEditorSaveState);
            RootNode.AddChild(stageEditorLoadState);
            CurrentNode = RootNode;
        }
    }
}