using Frontier.DebugTools.StageEditor;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;
using static UnityEngine.Rendering.DebugUI.Table;

namespace Frontier.DebugTools
{
    public class StageEditorEditingState : EditorStateBase
    {
        /// <summary>
        /// 遷移先を示すタグ
        /// </summary>
        enum TransitTag
        {
            Save = 0,
            Load,
        }

        private Action<int, int> PlaceTileCallback;
        private Func<string, bool> LoadStageCallback;

        private HierarchyBuilderBase _hierarchyBld      = null;
        private StageData _stageData                    = null;
        private GridCursorController _gridCursorCtrl    = null;
        // private ref int _refSelectedType;
        private StageEditorController.RefParams _refParams  = null;

        public GameObject[] tilePrefabs;
        private string _editFileName = "test_stage"; // 編集するステージファイル名

        [Inject]
        public void Construct( HierarchyBuilderBase hierarchyBld, StageData stageData, GridCursorController gridCursorCtrl, StageEditorController.RefParams refParams )
        {
            _hierarchyBld   = hierarchyBld;
            _stageData      = stageData;
            _gridCursorCtrl = gridCursorCtrl;
            _refParams      = refParams;
        }

        override public bool Update()
        {
            return (0 <= TransitIndex);
        }

        public void SetCallbacks(Action<int, int> placeTileCallback, Func<string, bool> loadStageCallback)
        {
            PlaceTileCallback = placeTileCallback;
            LoadStageCallback = loadStageCallback;
        }

        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR, "SELECT",    CanAcceptDirection, new AcceptDirectionInput(AcceptDirection), 0.1f, hashCode),
                (GuideIcon.CONFIRM, "CHANGE",       CanAcceptConfirm,   new AcceptBooleanInput(AcceptConfirm), 0.0f, hashCode),
                (GuideIcon.OPT1, "LOAD",            CanAcceptOptional1, new AcceptBooleanInput(AcceptOptional1), 0.0f, hashCode),
                (GuideIcon.OPT2, "SAVE",            CanAcceptOptional2, new AcceptBooleanInput(AcceptOptional2), 0.0f, hashCode),
                (GuideIcon.SUB2, "ADD TILE",        CanAcceptSub2, new AcceptBooleanInput(AcceptSub2), 0.0f, hashCode),
                (GuideIcon.SUB1, "SUB TILE",        CanAcceptSub1, new AcceptBooleanInput(AcceptSub1), 0.0f, hashCode),
                (GuideIcon.SUB3, "SUB HEIGHT",      CanAcceptSub3, new AcceptBooleanInput(AcceptSub3), 0.0f, hashCode),
                (GuideIcon.SUB4, "ADD HEIGHT",      CanAcceptSub4, new AcceptBooleanInput(AcceptSub4), 0.0f, hashCode)
            );
        }

        override protected bool CanAcceptDirection() { return true; }
        override protected bool CanAcceptConfirm() { return true; }
        protected override bool CanAcceptOptional1() { return true; }
        protected override bool CanAcceptOptional2() { return true; }
        override protected bool CanAcceptSub1() { return true; }
        override protected bool CanAcceptSub2() { return true; }
        override protected bool CanAcceptSub3() { return true; }
        override protected bool CanAcceptSub4() { return true; }
        override protected bool AcceptDirection(Direction dir)
        {

            if (dir == Direction.NONE) return false;

            if (dir == Direction.RIGHT) _gridCursorCtrl.Right();
            else if (dir == Direction.LEFT) _gridCursorCtrl.Left();
            else if (dir == Direction.FORWARD) _gridCursorCtrl.Up();
            else if (dir == Direction.BACK) _gridCursorCtrl.Down();

            return true;
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptConfirm(bool isInput)
        {
            if (isInput)
            {
                PlaceTileCallback(_gridCursorCtrl.X(), _gridCursorCtrl.Y());

                return true;
            }

            return false;
        }

        /// <summary>
        /// オプション入力1を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptOptional1(bool isInput)
        {
            if (!isInput) return false;

            if (!LoadStageCallback(_editFileName))
            {
                return false;
            }

            TransitIndex = (int)TransitTag.Load;

            return true;
        }

        /// <summary>
        /// オプション入力2を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptOptional2(bool isInput)
        {
            if (!isInput) return false;

            if (!StageDataSerializer.Save(_stageData, _editFileName))
            {
                return false;
            }

            TransitIndex = (int)TransitTag.Save;

            return true;
        }

        override protected bool AcceptSub1(bool isInput)
        {
            if (!isInput) return false;

            _refParams.SelectedType = Math.Clamp(_refParams.SelectedType - 1, 0, (int)TileType.NUM);

            return true;
        }

        override protected bool AcceptSub2(bool isInput)
        {
            if (!isInput) return false;

            _refParams.SelectedType = Math.Clamp(_refParams.SelectedType + 1, 0, (int)TileType.NUM);

            return true;
        }

        override protected bool AcceptSub3(bool isInput)
        {
            if (!isInput) return false;

            _refParams.SelectedHeight = Mathf.Clamp((float)_refParams.SelectedHeight - 0.5f, 0.0f, 5.0f);

            return true;
        }

        override protected bool AcceptSub4(bool isInput)
        {
            if (!isInput) return false;

            _refParams.SelectedHeight = Mathf.Clamp((float)_refParams.SelectedHeight + 0.5f, 0.0f, 5.0f);

            return true;
        }
    }
}