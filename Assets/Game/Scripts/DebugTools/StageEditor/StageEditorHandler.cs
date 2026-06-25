using System;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorHandler : EditorHandlerBase
    {
        private Action<EditActionContext> PlaceTileCallback;
        private Action<EditActionContext> ResizeTileGridCallback;
        private Action<EditActionContext> ToggleDeployableCallback;
        private Action<EditActionContext> PlaceEnemyCallback;
        private Action<EditActionContext> EditEnemyCallback;
        private Action<EditActionContext> DeleteEnemyCallback;
        private Action<EditActionContext> PlaceStagePropCallback;
        private Action<EditActionContext> EditStagePropCallback;
        private Action<EditActionContext> DeleteStagePropCallback;
        private Func<string, bool> SaveStageCallback;
        private Func<string, bool> LoadStageCallback;
        private Func<int, StageEditMode> ChangeEditModeCallback;

        private StageEditorUI _stageEditorView   = null;

        public void Init( StageEditorUI stageEditorView, Action<EditActionContext> placeTileCb, Action<EditActionContext> risizeTileGridCb, Action<EditActionContext> toggleDeployableCb, Action<EditActionContext> placeEnemyCb, Action<EditActionContext> editEnemyCb, Action<EditActionContext> deleteEnemyCb, Action<EditActionContext> placeStagePropCb, Action<EditActionContext> editStagePropCb, Action<EditActionContext> deleteStagePropCb, Func<string, bool> saveStageCb, Func<string, bool> loadStageCb, Func<int, StageEditMode> changeEditModeCb )
        {
            _stageEditorView            = stageEditorView;
            PlaceTileCallback           = placeTileCb;
            ResizeTileGridCallback      = risizeTileGridCb;
            ToggleDeployableCallback    = toggleDeployableCb;
            PlaceEnemyCallback          = placeEnemyCb;
            EditEnemyCallback           = editEnemyCb;
            DeleteEnemyCallback         = deleteEnemyCb;
            PlaceStagePropCallback      = placeStagePropCb;
            EditStagePropCallback       = editStagePropCb;
            DeleteStagePropCallback     = deleteStagePropCb;
            SaveStageCallback           = saveStageCb;
            LoadStageCallback           = loadStageCb;
            ChangeEditModeCallback      = changeEditModeCb;

            base.Init();
        }

        protected override void CreateTree()
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
            stageEditorEditingState.SetCallbacks(PlaceTileCallback, ResizeTileGridCallback, ToggleDeployableCallback, PlaceEnemyCallback, EditEnemyCallback, DeleteEnemyCallback, PlaceStagePropCallback, EditStagePropCallback, DeleteStagePropCallback, ChangeEditModeCallback);

            var stageEditorSaveState = _hierarchyBld.InstantiateWithDiContainer<StageEditorSaveState>(false);
            stageEditorSaveState.SetCallbacks( SaveStageCallback, _stageEditorView.SetMessageWord );

            var stageEditorLoadState = _hierarchyBld.InstantiateWithDiContainer<StageEditorLoadState>(false);
            stageEditorLoadState.SetCallbacks( LoadStageCallback, _stageEditorView.SetMessageWord);

            var stageEditorEditFileNameState = _hierarchyBld.InstantiateWithDiContainer<StageEditorEditFileNameState>( false );

            // 遷移木の作成
            RootNode = stageEditorEditingState;
            RootNode.AddChild(stageEditorSaveState);
            RootNode.AddChild(stageEditorLoadState);
            RootNode.AddChild(stageEditorEditFileNameState);
            CurrentNode = RootNode;
        }
    }
}

#endif // UNITY_EDITOR