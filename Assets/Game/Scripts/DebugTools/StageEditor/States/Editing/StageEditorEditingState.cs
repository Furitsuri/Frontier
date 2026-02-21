using Frontier.Stage;
using System;
using UnityEngine;
using Zenject;
using static Constants;
using static InputCode;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
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
            EditFileName,
        }

        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private GridCursorController _gridCursorCtrl   = null;

        private Action<int, int> PlaceTileCallback;
        private Action<int, int> ResizeTileGridCallback;
        private Action<int, int> ToggleDeployableCallback;
        private Func <int, StageEditMode> ChangeEditModeCallback;

        private StageEditorEditBase _currentEdit    = null;
        private StageEditorEditBase[] _editClasses  = null;
        private Action<int, int>[]  _editCallbacks  = null;
        private InputCode[] _sub1sub2InputCode      = null;
        private InputCode[] _sub3sub4InputCode      = null;
        private StageEditMode _editMode             = StageEditMode.NONE;

        public GameObject[] tilePrefabs;

        public void SetCallbacks( Action<int, int> placeTileCb, Action<int, int> risizeTileGridCb, Action<int, int> toggleDeployableCb, Func<int, StageEditMode> changeEditModeCb )
        {
            PlaceTileCallback           = placeTileCb;
            ResizeTileGridCallback      = risizeTileGridCb;
            ToggleDeployableCallback    = toggleDeployableCb;
            ChangeEditModeCallback      = changeEditModeCb;
            _editMode                   = ChangeEditModeCallback(0);  // コールバック設定の際に0を指定してコールすることで現在のeditModeを設定
        }

        public override void Init()
        {
            base.Init();

            // エディットモード毎に編集出来る内容を切り替えるため、各エディットクラスを配列内に挿入
            _editClasses = new StageEditorEditBase[( int ) StageEditMode.NUM]
            {
                _hierarchyBld.InstantiateWithDiContainer<StageEditorEditTileInformation>(false),
                _hierarchyBld.InstantiateWithDiContainer<StageEditorEditRowAndColumn>(false),
                _hierarchyBld.InstantiateWithDiContainer<StageEditorEditDeployableTile>(false)
            };

            _editCallbacks = new Action<int, int>[( int ) StageEditMode.NUM]
            {
                PlaceTileCallback,
                ResizeTileGridCallback,
                ToggleDeployableCallback
            };

            _currentEdit = _editClasses[(int)_editMode];
            _currentEdit.Init( _editCallbacks[( int ) _editMode] );

            int hashCode = GetInputCodeHash();

            _sub1sub2InputCode = new InputCode[( int )StageEditMode.NUM]
            {
                (new GuideIcon[] { GuideIcon.SUB1, GuideIcon.SUB2 }, "CHANGE MATERIAL", new EnableCallback[] { CanAcceptInputAlways, CanAcceptInputAlways }, new IAcceptInputBase[] { new AcceptContextInput( AcceptSub1 ), new AcceptContextInput( AcceptSub2 ) }, 0.0f, hashCode),
                (new GuideIcon[] { GuideIcon.SUB1, GuideIcon.SUB2 }, "CHANGE ROW NUM",  new EnableCallback[] { CanAcceptSub1, CanAcceptSub2 }, new IAcceptInputBase[] { new AcceptContextInput( AcceptSub1 ), new AcceptContextInput( AcceptSub2 ) }, 0.0f, hashCode),
                (null),
            };

            _sub3sub4InputCode = new InputCode[( int )StageEditMode.NUM]
            {
                (new GuideIcon[] { GuideIcon.SUB3, GuideIcon.SUB4 }, "CHANGE HEIGHT",       new EnableCallback[] { CanAcceptSub3, CanAcceptSub4 }, new IAcceptInputBase[] { new AcceptContextInput( AcceptSub3 ), new AcceptContextInput( AcceptSub4 ) }, 0.0f, hashCode),
                (new GuideIcon[] { GuideIcon.SUB3, GuideIcon.SUB4 }, "CHANGE COLUMN NUM",   new EnableCallback[] { CanAcceptSub3, CanAcceptSub4 }, new IAcceptInputBase[] { new AcceptContextInput( AcceptSub3 ), new AcceptContextInput( AcceptSub4 ) }, 0.0f, hashCode),
                (null),
            };
        }

        public override bool Update()
        {
            _currentEdit.Update();

            return (0 <= TransitIndex);
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR, "SELECT",    CanAcceptInputAlways, new AcceptContextInput( AcceptDirection ), 0.1f, hashCode),
                (GuideIcon.CONFIRM, "APPLY",        CanAcceptInputAlways, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
                (new GuideIcon[] { GuideIcon.TOOL, GuideIcon.INFO }, "MODE CHANGE", new EnableCallback[] { CanAcceptTool, CanAcceptInfo }, new IAcceptInputBase[] { new AcceptContextInput( AcceptTool ), new AcceptContextInput( AcceptInfo ) }, 0.0f, hashCode),
                (GuideIcon.OPT1, "LOAD",            CanAcceptInputAlways, new AcceptContextInput( AcceptOptional1 ), 0.0f, hashCode),
                (GuideIcon.OPT2, "SAVE",            CanAcceptInputAlways, new AcceptContextInput( AcceptOptional2 ), 0.0f, hashCode),
                _sub1sub2InputCode[(int)_editMode]?.Clone(),    // _sub1sub2InputCode[(int)_editMode]がnullの場合は、そのままnullを渡す
                _sub3sub4InputCode[(int)_editMode]?.Clone(),
                (GuideIcon.DEBUG_MENU, "FILE NAME", CanAcceptInputAlways, new AcceptContextInput( AcceptDebugTransition ), 0.0f, hashCode)
            );
        }

        protected override bool CanAcceptTool() { return 0 < (int)_editMode ; }
        protected override bool CanAcceptInfo() { return ( int )_editMode < (int)StageEditMode.NUM - 1; }
        protected override bool CanAcceptSub1() { return _currentEdit.CanAcceptSub1(); }
        protected override bool CanAcceptSub2() { return _currentEdit.CanAcceptSub2(); }
        protected override bool CanAcceptSub3() { return _currentEdit.CanAcceptSub3(); }
        protected override bool CanAcceptSub4() { return _currentEdit.CanAcceptSub4(); }

        protected override bool AcceptDirection( InputContext context )
        {

            if( context.Cursor == Direction.NONE ) { return false; }

            if( context.Cursor == Direction.RIGHT )         { _gridCursorCtrl.Right();  }
            else if( context.Cursor == Direction.LEFT )     { _gridCursorCtrl.Left();   }
            else if( context.Cursor == Direction.FORWARD )  { _gridCursorCtrl.Up();     }
            else if( context.Cursor == Direction.BACK )     { _gridCursorCtrl.Down();   }

            return true;
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptConfirm( InputContext context )
        {
            return _currentEdit.AcceptConfirm( context );
        }

        /// <summary>
        /// エディットモードを一つ前のモードに変更します
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptTool( InputContext context )
        {
            if ( base.AcceptTool( context ) )
            {
                _editMode = ChangeEditModeCallback( -1 );
                _inputFcd.UnregisterInputCodes();
                RegisterInputCodes();
                _currentEdit = _editClasses[( int )_editMode];
                _currentEdit.Init( _editCallbacks[( int )_editMode] );
                return true;
            }

            return false;
        }

        /// <summary>
        /// エディットモードを一つ後ろのモードに変更します
        /// </summary>
        /// <param name="isInput">情報画面入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            _editMode = ChangeEditModeCallback( 1 );
            _inputFcd.UnregisterInputCodes();
            RegisterInputCodes();
            _currentEdit = _editClasses[( int ) _editMode];
            _currentEdit.Init( _editCallbacks[( int ) _editMode] );
            return true;
        }

        /// <summary>
        /// オプション入力1を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptOptional1( InputContext context )
        {
            if( !base.AcceptOptional1( context ) ) { return false; }

            TransitState( ( int ) TransitTag.Load );

            return true;
        }

        /// <summary>
        /// オプション入力2を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptOptional2(InputContext context)
        {
            if( !base.AcceptOptional2( context ) ) { return false; }

            TransitState( ( int ) TransitTag.Save );

            return true;
        }

        protected override bool AcceptSub1( InputContext context ) { return _currentEdit.AcceptSub1( context ); }
        protected override bool AcceptSub2( InputContext context ) { return _currentEdit.AcceptSub2( context ); }
        protected override bool AcceptSub3( InputContext context ) { return _currentEdit.AcceptSub3( context ); }
        protected override bool AcceptSub4( InputContext context ) { return _currentEdit.AcceptSub4( context ); }

        /// <summary>
        /// ファイルネーム編集へ遷移させます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        protected override bool AcceptDebugTransition( InputContext context )
        {
            if( !base.AcceptDebugTransition( context ) ) { return false; }

            TransitState( ( int ) TransitTag.EditFileName );
            return true;
        }
    }
}

#endif // UNITY_EDITOR