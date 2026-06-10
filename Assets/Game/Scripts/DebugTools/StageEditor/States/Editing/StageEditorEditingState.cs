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
        [Inject] private GridCursor _gridCursor   = null;

        private Action<EditActionContext> PlaceTileCallback;
        private Action<EditActionContext> ResizeTileGridCallback;
        private Action<EditActionContext> ToggleDeployableCallback;
        private Action<EditActionContext> PlaceEnemyCallback;
        private Action<EditActionContext> EditEnemyCallback;
        private Func <int, StageEditMode> ChangeEditModeCallback;

        private StageEditorEditBase _currentEdit            = null;
        private StageEditorEditBase[] _editClasses          = null;
        private Action<EditActionContext>[]  _editCallbacks  = null;
        private StageEditMode _editMode                     = StageEditMode.NONE;

        public GameObject[] tilePrefabs;

        public void SetCallbacks( Action<EditActionContext> placeTileCb, Action<EditActionContext> risizeTileGridCb, Action<EditActionContext> toggleDeployableCb, Action<EditActionContext> placeEnemyCb, Action<EditActionContext> editEnemyCb, Func<int, StageEditMode> changeEditModeCb )
        {
            PlaceTileCallback           = placeTileCb;
            ResizeTileGridCallback      = risizeTileGridCb;
            ToggleDeployableCallback    = toggleDeployableCb;
            PlaceEnemyCallback          = placeEnemyCb;
            EditEnemyCallback           = editEnemyCb;
            ChangeEditModeCallback      = changeEditModeCb;
            _editMode                   = ChangeEditModeCallback(0);  // コールバック設定の際に0を指定してコールすることで現在のeditModeを設定
        }

        public override void Init( object context )
        {
            base.Init( context );

            // エディットモード毎に編集出来る内容を切り替えるため、各エディットクラスを配列内に挿入
            _editClasses = new StageEditorEditBase[( int ) StageEditMode.NUM]
            {
                _hierarchyBld.InstantiateWithDiContainer<StageEditorEditTileInformation>(false),
                _hierarchyBld.InstantiateWithDiContainer<StageEditorEditRowAndColumn>(false),
                _hierarchyBld.InstantiateWithDiContainer<StageEditorEditDeployableTile>(false),
                _hierarchyBld.InstantiateWithDiContainer<StageEditorEditEnemyCursor>(false),
            };

            // EDIT_ENEMY の StageEditorEditEnemyCursor に両コールバックを渡す
            var enemyCursor = (StageEditorEditEnemyCursor)_editClasses[( int ) StageEditMode.EDIT_ENEMY];
            enemyCursor.SetEnemyCallbacks( PlaceEnemyCallback, EditEnemyCallback );

            _editCallbacks = new Action<EditActionContext>[( int ) StageEditMode.NUM]
            {
                PlaceTileCallback,
                ResizeTileGridCallback,
                ToggleDeployableCallback,
                PlaceEnemyCallback,     // カーソルクラスは内部で使わないが配列は維持
            };

            _currentEdit = _editClasses[(int)_editMode];
            SetCurrentEditRefreshCallback();
            _currentEdit.Init( _editCallbacks[( int ) _editMode] );
        }

        public override bool Update()
        {
            _currentEdit.Update();

            return (0 <= TransitIndex);
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            string sub12Label = _currentEdit.GetSub12Label();
            string sub34Label = _currentEdit.GetSub34Label();

            InputCode sub12Code = sub12Label != null
                ? (InputCode)(new GuideIcon[] { GuideIcon.SUB1, GuideIcon.SUB2 }, sub12Label,
                    new EnableCallback[] { CanAcceptSub1, CanAcceptSub2 },
                    new IAcceptInputBase[] { new AcceptContextInput( AcceptSub1 ), new AcceptContextInput( AcceptSub2 ) },
                    0.0f, hashCode)
                : null;

            InputCode sub34Code = sub34Label != null
                ? (InputCode)(new GuideIcon[] { GuideIcon.SUB3, GuideIcon.SUB4 }, sub34Label,
                    new EnableCallback[] { CanAcceptSub3, CanAcceptSub4 },
                    new IAcceptInputBase[] { new AcceptContextInput( AcceptSub3 ), new AcceptContextInput( AcceptSub4 ) },
                    0.0f, hashCode)
                : null;

            InputCode cancelCode = _currentEdit.CanAcceptCancel()
                ? (InputCode)(GuideIcon.CANCEL, "BACK", CanAcceptCancel, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
                : null;

            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR, "SELECT",    CanAcceptInputAlways, new AcceptContextInput( AcceptDirection ), 0.1f, hashCode),
                (GuideIcon.CONFIRM, "APPLY",        CanAcceptInputAlways, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
                (new GuideIcon[] { GuideIcon.TOOL, GuideIcon.INFO }, "MODE\nCHANGE", new EnableCallback[] { CanAcceptTool, CanAcceptInfo }, new IAcceptInputBase[] { new AcceptContextInput( AcceptTool ), new AcceptContextInput( AcceptInfo ) }, 0.0f, hashCode),
                (GuideIcon.OPT1, "LOAD",            CanAcceptInputAlways, new AcceptContextInput( AcceptOptional1 ), 0.0f, hashCode),
                (GuideIcon.OPT2, "SAVE",            CanAcceptInputAlways, new AcceptContextInput( AcceptOptional2 ), 0.0f, hashCode),
                sub12Code,
                sub34Code,
                cancelCode,
                (GuideIcon.DEBUG_MENU, "FILE\nNAME", CanAcceptInputAlways, new AcceptContextInput( AcceptDebugTransition ), 0.0f, hashCode)
            );
        }

        protected override bool CanAcceptTool() { return 0 < (int)_editMode ; }
        protected override bool CanAcceptInfo() { return ( int )_editMode < (int)StageEditMode.NUM - 1; }
        protected override bool CanAcceptCancel() { return _currentEdit.CanAcceptCancel(); }
        protected override bool CanAcceptSub1() { return _currentEdit.CanAcceptSub1(); }
        protected override bool CanAcceptSub2() { return _currentEdit.CanAcceptSub2(); }
        protected override bool CanAcceptSub3() { return _currentEdit.CanAcceptSub3(); }
        protected override bool CanAcceptSub4() { return _currentEdit.CanAcceptSub4(); }

        protected override bool AcceptDirection( InputContext context )
        {

            if( context.Cursor == Direction.NONE ) { return false; }

            _gridCursor.Move( context.Cursor );

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
        /// キャンセル入力を受け取った際の処理を行います（サブモードからカーソルモードへ戻ります）
        /// </summary>
        protected override bool AcceptCancel( InputContext context )
        {
            return _currentEdit.AcceptCancel( context );
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
                _currentEdit.Exit();
                _editMode = ChangeEditModeCallback( -1 );
                _inputFcd.UnregisterInputCodes();
                _currentEdit = _editClasses[( int )_editMode];
                SetCurrentEditRefreshCallback();
                _currentEdit.Init( _editCallbacks[( int )_editMode] );
                RegisterInputCodes();
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

            _currentEdit.Exit();
            _editMode = ChangeEditModeCallback( 1 );
            _inputFcd.UnregisterInputCodes();
            _currentEdit = _editClasses[( int ) _editMode];
            SetCurrentEditRefreshCallback();
            _currentEdit.Init( _editCallbacks[( int ) _editMode] );
            RegisterInputCodes();
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

        /// <summary>
        /// 現在の _currentEdit に入力コード再登録コールバックを設定します
        /// </summary>
        private void SetCurrentEditRefreshCallback()
        {
            _currentEdit.RefreshInputCodes = () =>
            {
                _inputFcd.UnregisterInputCodes();
                RegisterInputCodes();
            };
        }
    }
}

#endif // UNITY_EDITOR
