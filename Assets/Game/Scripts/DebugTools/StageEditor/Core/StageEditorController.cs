using Frontier.Stage;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEditorInternal;
using UnityEngine;
using Zenject;
using static Constants;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// ステージエディターのコントローラー
    /// </summary>
    public class StageEditorController : FocusRoutineBase
    {
        public class RefParams
        {
            public StageEditMode EditMode   = StageEditMode.EDIT_TILE;  // 編集モード
            public int SelectedType         = 0;                        // 選択中のタイルタイプ
            public float SelectedHeight     = 0;                        // 選択中のタイル高さ
        }

        [Header("Editor Settings")]
        public int row    = 10;
        public int column = 10;

        [Header("Prefabs")]
        [SerializeField]
        public GameObject[] tilePrefabs;
        [SerializeField]
        public GameObject cursorPrefab;

        private InputFacade _inputFcd;
        private HierarchyBuilderBase _hierarchyBld;
        private IUiSystem _uiSystem;
        private Camera _mainCamera;
        private StageEditorHandler _stageEditorHandler  = null;
        private StageEditorPresenter _stageEditorView   = null;
        private StageData _stageData                    = null;
        private GridCursorController _gridCursorCtrl    = null;
        private RefParams _refParams                    = null;
        private StageEditMode _editMode                 = StageEditMode.EDIT_TILE;
        private Vector3 offset                          = new Vector3(0, 5, -5);    // ターゲットからの相対位置

        [Inject]
        public void Construct(InputFacade inputFacade, HierarchyBuilderBase hierarchyBld, IUiSystem uiSystem)
        {
            _inputFcd       = inputFacade;
            _hierarchyBld   = hierarchyBld;
            _uiSystem       = uiSystem;
        }

        private void CreateCursor()
        {
            _gridCursorCtrl = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<GridCursorController>(cursorPrefab, true, true, "GridCursorController");
            _gridCursorCtrl.Init(0, _stageData);
        }

        /// <summary>
        /// 指定された位置にタイルを設置します
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void PlaceTile(int x, int y)
        {
            _stageData.GetTile(x, y).Dispose(); // 既存のタイルを破棄
            _stageData.GetTile(x, y).SetTileTypeAndHeight((TileType)_refParams.SelectedType, _refParams.SelectedHeight);
            _stageData.GetTile(x, y).InstantiateTileInfo( x + y * _stageData.GridRowNum, _stageData.GridRowNum, _hierarchyBld );
            _stageData.GetTile(x, y).InstantiateTileBhv( x, y,  tilePrefabs, _hierarchyBld );
            _stageData.GetTile(x, y).InstantiateTileMesh( _hierarchyBld );
        }

        private void UpdateCamera(int x, int y)
        {
            if (_mainCamera == null) return;
            // カメラの位置を更新
            Vector3 targetPosition = _gridCursorCtrl.GetPosition() + offset;
            _mainCamera.transform.position = targetPosition;
            _mainCamera.transform.LookAt(_gridCursorCtrl.GetPosition());
        }

        private void UpdateTileVisual(int x, int y)
        {
            // 選択中タイルの見た目の強調表示など
        }

        /// <summary>
        /// ステージを作成します
        /// </summary>
        private void CreateStage()
        {
            if ( _stageData == null )
            {
                _stageData = _hierarchyBld.InstantiateWithDiContainer<StageData>( true );
                _stageData.Init( row, column );
            }

            for ( int y = 0; y < column; y++ )
            {
                for ( int x = 0; x < row; x++ )
                {
                    _stageData.SetTile( x, y, _hierarchyBld.InstantiateWithDiContainer<StageTileData>( false ) );
                    _stageData.GetTile( x, y ).InstantiateTileInfo( x + y * _stageData.GridRowNum, _stageData.GridRowNum, _hierarchyBld );
                    _stageData.GetTile( x, y ).InstantiateTileBhv( x, y, tilePrefabs, _hierarchyBld );
                    _stageData.GetTile( x, y ).InstantiateTileMesh( _hierarchyBld );
                }
            }
        }

        /// <summary>
        /// エディットモードを変更します
        /// </summary>
        /// <param name="add">モードの変更に加算する値</param>
        private StageEditMode ChangeEditMode( int add )
        {
            var nextMode    = Math.Clamp( ( int )_editMode + add, 0, (int)StageEditMode.NUM - 1 );
            _editMode       = ( StageEditMode )nextMode;

            return _editMode;
        }

        private bool LoadStage(string fileName)
        {
            var data = StageDataSerializer.Load(fileName);
            if (data == null) return false;

            _stageData.Dispose(); // 既存のステージデータを破棄
            
            // 簡易的に再ロード
            foreach (Transform child in transform) Destroy(child.gameObject);
            row         = data.GridRowNum;
            column      = data.GridColumnNum;
            _stageData.Init(row, column); // 新しいステージデータを初期化

            for (int y = 0; y < column; y++)
            {
                for (int x = 0; x < row; x++)
                {
                    var srcTile = data.GetTile(x, y);
                    _stageData.SetTile(x, y, _hierarchyBld.InstantiateWithDiContainer<StageTileData>(false));
                    var applyTile = _stageData.GetTile( x, y );
                    applyTile.SetTileTypeAndHeight( (TileType)srcTile.Type, srcTile.Height );
                    applyTile.InstantiateTileInfo( x + y * _stageData.GridRowNum, _stageData.GridRowNum, _hierarchyBld );
                    applyTile.InstantiateTileBhv( x, y, tilePrefabs, _hierarchyBld );
                    applyTile.InstantiateTileMesh( _hierarchyBld );
                }
            }

            _gridCursorCtrl.Init(0, _stageData);

            return true;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        override public void Init()
        {
            if ( null == _stageEditorView )
            {
                _stageEditorView = _uiSystem.DebugUi.StageEditorView;
                NullCheck.AssertNotNull( _stageEditorView );
            }

            if ( null == _stageEditorHandler )
            {
                _stageEditorHandler = _hierarchyBld.InstantiateWithDiContainer<StageEditorHandler>( false );
                NullCheck.AssertNotNull( _stageEditorHandler );
            }

            if ( null == _refParams )
            {
                _refParams = _hierarchyBld.InstantiateWithDiContainer<RefParams>( true );
            }

            _inputFcd.Init();           // 入力ファサードの初期化
            TileMaterialLibrary.Init(); // タイルマテリアルの初期化

            CreateStage();
            CreateCursor();

            _stageEditorHandler.Init( _stageEditorView, PlaceTile, LoadStage, ChangeEditMode );
            _stageEditorHandler.Run();

            _mainCamera = Camera.main;
        }

        override public void UpdateRoutine()
        {
            _stageEditorHandler.Update();

            UpdateCamera( _gridCursorCtrl.X(), _gridCursorCtrl.Y() );
            UpdateTileVisual( _gridCursorCtrl.X(), _gridCursorCtrl.Y() );
            _stageEditorView.UpdateText( _editMode, _refParams.SelectedType, _refParams.SelectedHeight );
        }

        override public void LateUpdateRoutine()
        {
            _stageEditorHandler.LateUpdate();
        }
    }
} // namespace Frontier.DebugTools.StageEditor

#endif // UNITY_EDITOR