using Frontier.DebugTools.StageEditor;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using Zenject;
using static Constants;
using static InputCode;
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
        private Func <int, StageEditMode> ChangeEditModeCallback;

        private HierarchyBuilderBase _hierarchyBld          = null;
        private StageData _stageData                        = null;
        private GridCursorController _gridCursorCtrl        = null;
        private StageEditorController.RefParams _refParams  = null;
        private InputCode[] _sub1sub2InputCode              = null;
        private InputCode[] _sub3sub4InputCode              = null;
        private StageEditMode _editMode                     = StageEditMode.NONE;

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

        public void SetCallbacks( Action<int, int> placeTileCb, Func<string, bool> loadStageCb, Func<int, StageEditMode> changeEditModeCb )
        {
            PlaceTileCallback       = placeTileCb;
            LoadStageCallback       = loadStageCb;
            ChangeEditModeCallback  = changeEditModeCb;

            _editMode = ChangeEditModeCallback(0);  // コールバック設定の際に0を指定してコールすることで現在のeditModeを設定
        }

        override public void Init()
        {
            base.Init();

            int hashCode = GetInputCodeHash();

            _sub1sub2InputCode = new InputCode[( int )StageEditMode.NUM]
            {
                (new GuideIcon[] { GuideIcon.SUB1, GuideIcon.SUB2 }, "CHANGE MATERIAL", new EnableCallback[] { CanAcceptInputAlways, CanAcceptInputAlways }, new IAcceptInputBase[] { new AcceptBooleanInput( AcceptSub1 ), new AcceptBooleanInput( AcceptSub2 ) }, 0.0f, hashCode),
                (new GuideIcon[] { GuideIcon.SUB1, GuideIcon.SUB2 }, "CHANGE ROW NUM",  new EnableCallback[] { CanAcceptSub1, CanAcceptSub2 }, new IAcceptInputBase[] { new AcceptBooleanInput( AcceptSub1 ), new AcceptBooleanInput( AcceptSub2 ) }, 0.0f, hashCode),
                (null),
            };

            _sub3sub4InputCode = new InputCode[( int )StageEditMode.NUM]
            {
                (new GuideIcon[] { GuideIcon.SUB3, GuideIcon.SUB4 }, "CHANGE HEIGHT",       new EnableCallback[] { CanAcceptSub3, CanAcceptSub4 }, new IAcceptInputBase[] { new AcceptBooleanInput( AcceptSub3 ), new AcceptBooleanInput( AcceptSub4 ) }, 0.0f, hashCode),
                (new GuideIcon[] { GuideIcon.SUB3, GuideIcon.SUB4 }, "CHANGE COLUMN NUM",   new EnableCallback[] { CanAcceptSub3, CanAcceptSub4 }, new IAcceptInputBase[] { new AcceptBooleanInput( AcceptSub3 ), new AcceptBooleanInput( AcceptSub4 ) }, 0.0f, hashCode),
                (null),
            };
        }

        override public bool Update()
        {
            return (0 <= TransitIndex);
        }

        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR, "SELECT",    CanAcceptInputAlways, new AcceptDirectionInput( AcceptDirection ), 0.1f, hashCode),
                (GuideIcon.CONFIRM, "APPLY",        CanAcceptInputAlways, new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
                (new GuideIcon[] { GuideIcon.TOOL, GuideIcon.INFO }, "MODE CHANGE", new EnableCallback[] { CanAcceptTool, CanAcceptInfo }, new IAcceptInputBase[] { new AcceptBooleanInput( AcceptTool ), new AcceptBooleanInput( AcceptInfo ) }, 0.0f, hashCode),
                (GuideIcon.OPT1, "LOAD",            CanAcceptInputAlways, new AcceptBooleanInput( AcceptOptional1 ), 0.0f, hashCode),
                (GuideIcon.OPT2, "SAVE",            CanAcceptInputAlways, new AcceptBooleanInput( AcceptOptional2 ), 0.0f, hashCode),
                _sub1sub2InputCode[(int)_editMode]?.Clone(),    // _sub1sub2InputCode[(int)_editMode]がnullの場合は、そのままnullを渡す
                _sub3sub4InputCode[(int)_editMode]?.Clone()
            );
        }

        override protected bool CanAcceptTool() { return 0 < (int)_editMode ; }
        override protected bool CanAcceptInfo() { return ( int )_editMode < (int)StageEditMode.NUM - 1; }
        override protected bool CanAcceptSub3() { return 0f < _refParams.SelectedHeight; }
        override protected bool CanAcceptSub4() { return _refParams.SelectedHeight < 5.0f; }

        override protected bool AcceptDirection(Direction dir)
        {

            if (dir == Direction.NONE) return false;

            if (dir == Direction.RIGHT)         _gridCursorCtrl.Right();
            else if (dir == Direction.LEFT)     _gridCursorCtrl.Left();
            else if (dir == Direction.FORWARD)  _gridCursorCtrl.Up();
            else if (dir == Direction.BACK)     _gridCursorCtrl.Down();

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
        /// ツール画面入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">ツール画面入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptTool( bool isInput )
        {
            if ( isInput )
            {
                _editMode = ChangeEditModeCallback( -1 );
                _inputFcd.UnregisterInputCodes();
                RegisterInputCodes();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 情報画面入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">情報画面入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptInfo( bool isInput )
        {
            if ( isInput )
            {
                _editMode = ChangeEditModeCallback( 1 );
                _inputFcd.UnregisterInputCodes();
                RegisterInputCodes();
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

        /// <summary>
        /// タイルタイプの値をデクリメントします。
        /// 値が負になった場合は最大値-1とすることでループさせます。
        /// </summary>
        /// <param name="isInput">入力の有無</param>
        /// <returns>入力受付の有無</returns>
        override protected bool AcceptSub1(bool isInput)
        {
            if (!isInput) return false;

            if ( --_refParams.SelectedType < 0 ) { _refParams.SelectedType = ( int )TileType.NUM - 1; }

            return true;
        }

        /// <summary>
        /// タイルタイプの値をインクリメントします。
        /// 値が最大値を超えた場合は0とすることでループさせます。
        /// </summary>
        /// <param name="isInput">入力の有無</param>
        /// <returns>入力受付の有無</returns>
        override protected bool AcceptSub2(bool isInput)
        {
            if (!isInput) return false;

            if( (int)TileType.NUM <= ++_refParams.SelectedType ) { _refParams.SelectedType = 0; }

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