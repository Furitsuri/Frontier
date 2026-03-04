using Frontier.Registries;
using Frontier.Stage;
using System;
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
        public class StageEditRefParams
        {
            public StageEditMode EditMode   = StageEditMode.EDIT_TILE;      // 編集モード
            public int MaxDeployableUnits   = DEPLOYABLE_UNIT_DEFAULT_NUM;  // 配置可能ユニット数
            public int Row                  = 10;                           // タイルの行数
            public int Col                  = 10;                           // タイルの列数
            public int SelectedType         = 0;                            // 選択中のタイルタイプ
            public float SelectedHeight     = 0;                            // 選択中のタイル高さ

            /// <summary>
            /// ステージデータの内容を適応させます
            /// </summary>
            /// <param name="stageData">参照するステージデータ</param>
            public void AdaptStageData( StageData stageData )
            {
                Row = stageData.TileRowNum;
                Col = stageData.TileColNum;
            }

            public void SetDeployableUnitsNum( int deployableUnitsNum )
            {
                MaxDeployableUnits = Math.Clamp( deployableUnitsNum, DEPLOYABLE_UNIT_MIN_NUM, DEPLOYABLE_UNIT_MAX_NUM );
            }
        }

        [Header("Prefabs")]
        [SerializeField]
        public GameObject[] tilePrefabs;
        [SerializeField]
        public GameObject cursorPrefab;
        [SerializeField]
        public GameObject stageFileLoaderPrefab;

        [Inject] private IUiSystem _uiSystem                    = null;
        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private InputFacade _inputFcd                  = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private PrefabRegistry _prefabReg              = null;

        private BattleCameraController _btlCamCtrl      = null;
        private Camera _mainCamera;
        private StageEditorHandler _stageEditorHandler  = null;
        private StageEditorUI _stageEditorView          = null;
        private StageFileLoader _stageFileLoader        = null;
        private GridCursorController _gridCursorCtrl    = null;
        private StageEditRefParams _refParams           = null;
        private Holder<string> _editFileName            = null;
        private StageEditMode _editMode                 = StageEditMode.EDIT_TILE;
        private Vector3 offset                          = new Vector3(0, 5, -5);    // ターゲットからの相対位置

        public Holder<string> EditFileName => _editFileName;

        /// <summary>
        /// 指定された位置にタイルを設置します
        /// </summary>
        private void PlaceTile( EditActionContext context )
        {
            var data = _stageDataProvider.CurrentData;

            bool isDeployable   = data.GetTile( context.X, context.Y ).StaticData().IsDeployable;   // 以前の配置可能状態を取得
            data.GetTile( context.X, context.Y ).Dispose();
            data.SetTile( context.X, context.Y, _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Tile>( tilePrefabs[0], true, false, $"Tile_X{context.X}_Y{context.Y}" ) );
            data.GetTile( context.X, context.Y ).Init( context.X, context.Y, isDeployable, _refParams.SelectedHeight, ( TileType ) _refParams.SelectedType );
        }

        /// <summary>
        /// タイルの行数、列数を編集します
        /// </summary>
        private void ResizeTileGrid( EditActionContext context )
        {
            StageData resizeStageData = _hierarchyBld.InstantiateWithDiContainer<StageData>(false);
            resizeStageData.Init( _stageDataProvider.CurrentData.MaxDeployableUnits, context.Y, context.X );

            int minRow = Mathf.Min( context.Y, _stageDataProvider.CurrentData.TileRowNum );
            int minCol = Mathf.Min( context.X, _stageDataProvider.CurrentData.TileColNum );

            for ( int i = 0; i < context.X; ++i )
            {
                for ( int j = 0; j < context.Y; ++j )
                {
                    // 変更前のステージデータと重複するタイルは変更前のものを複製する
                    if ( i < minCol && j < minRow )
                    {
                        resizeStageData.SetTile( i, j, _stageDataProvider.CurrentData.GetTile( i, j ).Clone( i, j, tilePrefabs ) );
                    }
                    else
                    {
                        // 追加で生成するタイルは、端のタイルを参照して作成する
                        if( minCol <= i && minRow <= j )
                        {
                            resizeStageData.SetTile( i, j, _stageDataProvider.CurrentData.GetTile( minCol - 1, minRow - 1 ).Clone( i, j, tilePrefabs ) );
                        }
                        else if( minCol <= i )
                        {
                            resizeStageData.SetTile( i, j, _stageDataProvider.CurrentData.GetTile( minCol - 1, j ).Clone( i, j, tilePrefabs ) );
                        }
                        else if( minRow <= j )
                        {
                            resizeStageData.SetTile( i, j, _stageDataProvider.CurrentData.GetTile( i, minRow - 1 ).Clone( i, j, tilePrefabs ) );
                        }
                    }
                }
            }

            _stageDataProvider.CurrentData.Dispose();
            _stageDataProvider.CurrentData = resizeStageData;      // 作成したデータを保持

            _gridCursorCtrl.Init( 0 );  // グリッドカーソル位置を初期化
        }

        /// <summary>
        /// 指定された位置のタイルの配置可能状態を切り替えます
        /// </summary>
        private void ToggleDeployable( EditActionContext context )
        {
            var data = _stageDataProvider.CurrentData;

            // 配置可能ユニット数の更新
            if( 0 < context.ExtraIntValues.Count )
            {
                data.SetMaxDeployableUnits( context.ExtraIntValues[0] );
            }

            if( context.X < 0 || context.Y < 0 ) { return; }   // タイルの位置が不正な場合は処理しない

            data.GetTile( context.X, context.Y ).StaticData().IsDeployable = !data.GetTile( context.X, context.Y ).StaticData().IsDeployable;
            data.GetTile( context.X, context.Y ).ApplyDeployableColor();
        }

        /// <summary>
        /// カメラの更新処理です
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
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
        private StageData CreateDefaultStage()
        {
            StageData stageData = _hierarchyBld.InstantiateWithDiContainer<StageData>( false );
            NullCheck.AssertNotNull( stageData, nameof(stageData) );
            stageData.Init( _refParams.MaxDeployableUnits, _refParams.Row, _refParams.Col );
            stageData.CreateDefaultTiles( tilePrefabs );

            return stageData;
        }

        /// <summary>
        /// エディットモードを変更します
        /// </summary>
        /// <param name="add">モードの変更に加算する値</param>
        private StageEditMode ChangeEditMode( int add )
        {
            var nextMode    = Math.Clamp( ( int )_editMode + add, 0, (int)StageEditMode.NUM - 1 );
            _editMode       = ( StageEditMode )nextMode;

            _stageEditorView.SwitchEditParamView( _editMode );   // モードに応じたパラメータを表示

            return _editMode;
        }

        /// <summary>
        /// 現在のステージデータを保存します
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool SaveStage( string fileName )
        {
            if( !StageDataSerializer.Save( _stageDataProvider.CurrentData, fileName ) )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// ステージを読み込みます
        /// </summary>
        /// <param name="fileName">読み込むステージのファイル名</param>
        /// <returns>読込の成否</returns>
        private bool LoadStage( string fileName )
        {
            if( _stageFileLoader.Load( fileName ) )
            {
                _gridCursorCtrl.Init( 0 );  // グリッドカーソルの位置をタイル番号0の地点に合わせる

                return true;
            }

            return false;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public override void Init()
        {
            LazyInject.GetOrCreate( ref _stageEditorView, () => _uiSystem.DebugUi.StageEditorView );
            LazyInject.GetOrCreate( ref _stageEditorHandler, () => _hierarchyBld.InstantiateWithDiContainer<StageEditorHandler>( false ) );
            LazyInject.GetOrCreate( ref _stageFileLoader, () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StageFileLoader>( stageFileLoaderPrefab, true, false, "StageFileLoader" ) );
            LazyInject.GetOrCreate( ref _refParams, () => _hierarchyBld.InstantiateWithDiContainer<StageEditRefParams>( true ) );
            LazyInject.GetOrCreate( ref _editFileName, () => new Holder<string>( "NewStage" ) );
            LazyInject.GetOrCreate( ref _btlCamCtrl, () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<BattleCameraController>( _prefabReg.BattleCameraPrefab, true, true, typeof( BattleCameraController ).Name ) );
            LazyInject.GetOrCreate( ref _gridCursorCtrl, () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<GridCursorController>( cursorPrefab, true, true, "GridCursorController" ) );

            _btlCamCtrl.Setup( false );

            _inputFcd.Init();           // 入力ファサードの初期化
            TileMaterialLibrary.Init(); // タイルマテリアルの初期化

            _stageDataProvider.CurrentData  = CreateDefaultStage();         // プロバイダーに登録
            _refParams.AdaptStageData( _stageDataProvider.CurrentData );    // 作成したステージデータの内容を参照パラメータに適応

            _gridCursorCtrl.Init( 0 );
            _stageEditorView.Init( EditFileName );
            _stageFileLoader.Init( tilePrefabs );

            _stageEditorHandler.Init( _stageEditorView, PlaceTile, ResizeTileGrid, ToggleDeployable, SaveStage, LoadStage, ChangeEditMode );
            _stageEditorHandler.Run();

            _btlCamCtrl.Init();

            _mainCamera = Camera.main;
        }

        public override void UpdateRoutine()
        {
            _stageEditorHandler.Update();

            UpdateCamera( _gridCursorCtrl.X(), _gridCursorCtrl.Y() );
            UpdateTileVisual( _gridCursorCtrl.X(), _gridCursorCtrl.Y() );
            _stageEditorView.UpdateModeText( _editMode, _refParams );
        }

        public override void LateUpdateRoutine()
        {
            _stageEditorHandler.LateUpdate();
        }
    }
} // namespace Frontier.DebugTools.StageEditor

#endif // UNITY_EDITOR