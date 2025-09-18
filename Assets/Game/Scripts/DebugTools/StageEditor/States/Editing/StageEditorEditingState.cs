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

namespace Frontier.DebugTools
{
    public class StageEditorEditingState : EditorStateBase
    {
        /// <summary>
        /// 遷移先を示すタグ
        /// </summary>
        private enum TransitTag
        {
            Save = 0,
            Load,
        }

        private Action<int, int> PlaceTileCallback;
        private Action<int, int> ResizeTileGridCallback;
        private Func<string, bool> LoadStageCallback;
        private Func <int, StageEditMode> ChangeEditModeCallback;

        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private GridCursorController _gridCursorCtrl   = null;

        private StageEditorEditBase _currentEdit            = null;
        private StageEditorEditBase[] _editClasses          = null;
        private InputCode[] _sub1sub2InputCode              = null;
        private InputCode[] _sub3sub4InputCode              = null;
        private StageEditMode _editMode                     = StageEditMode.NONE;

        public GameObject[] tilePrefabs;
        private string _editFileName = "test_stage"; // 編集するステージファイル名

        public void SetCallbacks( Action<int, int> placeTileCb, Action<int, int> risizeTileGridCb, Func<string, bool> loadStageCb, Func<int, StageEditMode> changeEditModeCb )
        {
            PlaceTileCallback       = placeTileCb;
            ResizeTileGridCallback  = risizeTileGridCb;
            LoadStageCallback       = loadStageCb;
            ChangeEditModeCallback  = changeEditModeCb;
            _editMode               = ChangeEditModeCallback(0);  // コールバック設定の際に0を指定してコールすることで現在のeditModeを設定
        }

        override public void Init()
        {
            base.Init();

            // エディットモード毎に編集出来る内容を切り替えるため、各エディットクラスを配列内に挿入
            _editClasses = new StageEditorEditBase[ (int)StageEditMode.NUM ]
            {
                _hierarchyBld.InstantiateWithDiContainer<StageEditorEditTileInformation>(false),
                _hierarchyBld.InstantiateWithDiContainer<StageEditorEditRowAndColumn>(false),
                _hierarchyBld.InstantiateWithDiContainer<StageEditorEditCharacterInitialPosition>(false)
            };

            _currentEdit = _editClasses[(int)_editMode];
            _currentEdit.Init( PlaceTileCallback, ResizeTileGridCallback );

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
            _currentEdit.Update();

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
        override protected bool CanAcceptSub1() { return _currentEdit.CanAcceptSub1(); }
        override protected bool CanAcceptSub2() { return _currentEdit.CanAcceptSub2(); }
        override protected bool CanAcceptSub3() { return _currentEdit.CanAcceptSub3(); }
        override protected bool CanAcceptSub4() { return _currentEdit.CanAcceptSub4(); }

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
            return _currentEdit.AcceptConfirm(isInput);
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
                _currentEdit = _editClasses[( int )_editMode];
                _currentEdit.Init( PlaceTileCallback, ResizeTileGridCallback );
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
                _currentEdit = _editClasses[( int )_editMode];
                _currentEdit.Init( PlaceTileCallback, ResizeTileGridCallback );
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

            if (!StageDataSerializer.Save(_stageDataProvider.CurrentData, _editFileName))
            {
                return false;
            }

            TransitIndex = (int)TransitTag.Save;

            return true;
        }

        override protected bool AcceptSub1(bool isInput ) { return _currentEdit.AcceptSub1( isInput ); }

        override protected bool AcceptSub2(bool isInput ) { return _currentEdit.AcceptSub2( isInput ); }

        override protected bool AcceptSub3(bool isInput ) { return _currentEdit.AcceptSub3( isInput ); }

        override protected bool AcceptSub4(bool isInput ) { return _currentEdit.AcceptSub4( isInput ); }
    }
}