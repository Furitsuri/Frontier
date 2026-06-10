using Frontier.Registries;
using Frontier.Stage;
using System;
using System.Collections.Generic;
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

            // --- 敵配置テンプレート ---
            public int EnemyLevel         = 1;
            public int EnemyMaxHP         = 100;
            public int EnemyAtk           = 10;
            public int EnemyDef           = 5;
            public int EnemyMoveRange     = 4;
            public int EnemyJumpForce     = 2;
            public int EnemyAtkRange      = 1;
            public int EnemyActGaugeMax   = 100;
            public int EnemyActRecovery   = 10;
            public int EnemyPrefab        = 0;
            public int EnemyThinkType     = 0;
            public int EnemyInitGridIndex = 0;
            public int EnemyInitDir       = ( int ) Direction.BACK;
            public int SelectedEnemyParamIndex = 0;

            /// <summary>配置済み敵の (グリッドインデックス → _enemyStatusList インデックス) マップ</summary>
            public Dictionary<int, int> GridIndexToEnemyListIndex = new Dictionary<int, int>();

            /// <summary>現在編集中の既存敵のグリッドインデックス。EditExisting サブモードでのみ有効。</summary>
            public int EditingEnemyGridIndex = -1;

            /// <summary>グリッドインデックスから敵データを _refParams に読み込むコールバック。Controller が設定します。</summary>
            public Func<int, bool> TryLoadEnemyAtGridIndex = null;

            /// <summary>NewPlacement / EditExisting サブモード中は true。EditParam パネルの表示制御に使用。</summary>
            public bool EnemySubModeActive = false;

            /// <summary>カーソル下に配置済み敵がいる場合 true。EnemyParamList パネルの表示制御に使用。</summary>
            public bool EnemyAtCursor = false;

            public static readonly string[] EnemyParamNames =
            {
                "Level", "MaxHP", "Atk", "Def", "MoveRange",
                "JumpForce", "AtkRange", "ActGaugeMax", "ActRecovery",
                "Prefab", "ThinkType", "InitGridIndex", "InitDir"
            };

            public int GetEnemyParamValue( int index )
            {
                return index switch
                {
                    0  => EnemyLevel,
                    1  => EnemyMaxHP,
                    2  => EnemyAtk,
                    3  => EnemyDef,
                    4  => EnemyMoveRange,
                    5  => EnemyJumpForce,
                    6  => EnemyAtkRange,
                    7  => EnemyActGaugeMax,
                    8  => EnemyActRecovery,
                    9  => EnemyPrefab,
                    10 => EnemyThinkType,
                    11 => EnemyInitGridIndex,
                    12 => EnemyInitDir,
                    _  => 0,
                };
            }

            /// <summary>
            /// パラメータの表示用文字列を返します。
            /// InitDir (index=12) は Direction の enum 名で返します。
            /// </summary>
            public string GetEnemyParamDisplayString( int index )
            {
                if ( index == 12 )
                {
                    return ( ( Direction ) EnemyInitDir ).ToString();
                }
                return GetEnemyParamValue( index ).ToString();
            }

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
        private GridCursor _gridCursor                  = null;
        private StageEditRefParams _refParams           = null;
        private Holder<string> _editFileName            = null;
        private StageEditMode _editMode                 = StageEditMode.EDIT_TILE;
        private Vector3 offset                          = new Vector3(0, 5, -5);    // ターゲットからの相対位置
        private Func<int, int>[] _gridDirectionMoveCallbacks;

        // 登録済み敵ステータスデータ一覧
        private System.Collections.Generic.List<Frontier.Loaders.BattleFileLoader.CharacterStatusData> _enemyStatusList
            = new System.Collections.Generic.List<Frontier.Loaders.BattleFileLoader.CharacterStatusData>();

        public Holder<string> EditFileName => _editFileName;

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
            LazyInject.GetOrCreate( ref _gridCursor, () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<GridCursor>( cursorPrefab, new object[] { Color.yellow, true }, true, true, "GridCursor" ) );

            _btlCamCtrl.Setup( false );
            _inputFcd.Setup();

            InitCallbacks();

            _inputFcd.Init();           // 入力ファサードの初期化
            TileMaterialLibrary.Init(); // タイルマテリアルの初期化

            _stageDataProvider.CurrentData = CreateDefaultStage();         // プロバイダーに登録
            _refParams.AdaptStageData( _stageDataProvider.CurrentData );    // 作成したステージデータの内容を参照パラメータに適応

            _gridCursor.Init( 0, _gridDirectionMoveCallbacks );
            _stageEditorView.Init( EditFileName );
            _stageFileLoader.Init( tilePrefabs );

            _refParams.TryLoadEnemyAtGridIndex = TryLoadEnemyAtGridIndex;
            _stageEditorHandler.Init( _stageEditorView, PlaceTile, ResizeTileGrid, ToggleDeployable, PlaceEnemy, EditEnemy, SaveStage, LoadStage, ChangeEditMode );
            _stageEditorHandler.Enter();

            _btlCamCtrl.Init();

            _mainCamera = Camera.main;
        }

        public override void UpdateRoutine()
        {
            _stageEditorHandler.Update();

            UpdateCamera( _gridCursor.X(), _gridCursor.Y() );
            UpdateTileVisual( _gridCursor.X(), _gridCursor.Y() );
            _stageEditorView.UpdateModeText( _editMode, _refParams );
        }

        public override void LateUpdateRoutine()
        {
            _stageEditorHandler.LateUpdate();
        }

        private void InitCallbacks()
        {
            _gridDirectionMoveCallbacks = new Func<int, int>[( int ) Direction.NUM]
            {
                // Direction.FORWARD
                ( tileIndex ) =>
                {
                    tileIndex += _stageDataProvider.CurrentData.TileColNum;
                    if( _stageDataProvider.CurrentData.GetTileTotalNum() <= tileIndex )
                    {
                        tileIndex = tileIndex % ( _stageDataProvider.CurrentData.GetTileTotalNum() );
                    }

                    return tileIndex;
                },
                // Direction.RIGHT
                ( tileIndex ) =>
                {
                    tileIndex++;
                    if( tileIndex % _stageDataProvider.CurrentData.TileColNum == 0 )
                    {
                        tileIndex -= _stageDataProvider.CurrentData.TileColNum;
                    }
                    return tileIndex;
                },
                // Direction.BACK
                ( tileIndex ) =>
                {
                    tileIndex -= _stageDataProvider.CurrentData.TileColNum;
                    if( tileIndex < 0 )
                    {
                        tileIndex += _stageDataProvider.CurrentData.GetTileTotalNum();
                    }
                    return tileIndex;
                },
                // Direction.LEFT
                ( tileIndex ) =>
                {
                    tileIndex--;
                    if( ( tileIndex + 1 ) % _stageDataProvider.CurrentData.TileColNum == 0 )
                    {
                        tileIndex += _stageDataProvider.CurrentData.TileColNum;
                    }
                    return tileIndex;
                }
            };
        }

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

            _gridCursor.Init( 0, _gridDirectionMoveCallbacks );  // グリッドカーソル位置を初期化
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
        /// 敵ステータスデータをテンプレートから生成してリストに登録します
        /// </summary>
        private void PlaceEnemy( EditActionContext context )
        {
            var data = new Frontier.Loaders.BattleFileLoader.CharacterStatusData
            {
                CharacterTag    = ( int ) Frontier.Entities.CHARACTER_TAG.ENEMY,
                CharacterIndex  = _enemyStatusList.Count,
                Level           = _refParams.EnemyLevel,
                MaxHP           = _refParams.EnemyMaxHP,
                Atk             = _refParams.EnemyAtk,
                Def             = _refParams.EnemyDef,
                MoveRange       = _refParams.EnemyMoveRange,
                JumpForce       = _refParams.EnemyJumpForce,
                AtkRange        = _refParams.EnemyAtkRange,
                ActGaugeMax     = _refParams.EnemyActGaugeMax,
                ActRecovery     = _refParams.EnemyActRecovery,
                Prefab          = _refParams.EnemyPrefab,
                ThinkType       = _refParams.EnemyThinkType,
                InitGridIndex   = _refParams.EnemyInitGridIndex,
                InitDir         = _refParams.EnemyInitDir,
            };

            _enemyStatusList.Add( data );
            _refParams.GridIndexToEnemyListIndex[data.InitGridIndex] = data.CharacterIndex;
            Debug.Log( $"[StageEditor] 敵を登録しました (Index={data.CharacterIndex} Prefab={data.Prefab} Level={data.Level})" );
        }

        /// <summary>
        /// 配置済み敵のステータスデータを現在の _refParams 値で更新します。
        /// context.ExtraIntValues[0] に対象敵のグリッドインデックスが格納されていることを前提とします。
        /// </summary>
        private void EditEnemy( EditActionContext context )
        {
            if ( context.ExtraIntValues.Count == 0 ) return;

            int oldGridIndex = context.ExtraIntValues[0];
            int newGridIndex = context.ExtraIntValues.Count >= 2 ? context.ExtraIntValues[1] : oldGridIndex;

            if ( !_refParams.GridIndexToEnemyListIndex.TryGetValue( oldGridIndex, out int listIndex ) ) return;

            // グリッドインデックスが変わった場合は辞書キーを更新
            if ( newGridIndex != oldGridIndex )
            {
                _refParams.GridIndexToEnemyListIndex.Remove( oldGridIndex );
                _refParams.GridIndexToEnemyListIndex[newGridIndex] = listIndex;
            }

            var data = _enemyStatusList[listIndex];
            data.Level         = _refParams.EnemyLevel;
            data.MaxHP         = _refParams.EnemyMaxHP;
            data.Atk           = _refParams.EnemyAtk;
            data.Def           = _refParams.EnemyDef;
            data.MoveRange     = _refParams.EnemyMoveRange;
            data.JumpForce     = _refParams.EnemyJumpForce;
            data.AtkRange      = _refParams.EnemyAtkRange;
            data.ActGaugeMax   = _refParams.EnemyActGaugeMax;
            data.ActRecovery   = _refParams.EnemyActRecovery;
            data.Prefab        = _refParams.EnemyPrefab;
            data.ThinkType     = _refParams.EnemyThinkType;
            data.InitGridIndex = newGridIndex;
            data.InitDir       = _refParams.EnemyInitDir;
            _enemyStatusList[listIndex] = data;

            Debug.Log( $"[StageEditor] 敵を更新しました (ListIndex={listIndex} GridIndex={oldGridIndex}→{newGridIndex})" );
        }

        /// <summary>
        /// 指定グリッドインデックスに配置されている敵データを _refParams に読み込みます。
        /// </summary>
        private bool TryLoadEnemyAtGridIndex( int gridIndex )
        {
            if ( !_refParams.GridIndexToEnemyListIndex.TryGetValue( gridIndex, out int listIndex ) ) return false;

            var data = _enemyStatusList[listIndex];
            _refParams.EnemyLevel         = data.Level;
            _refParams.EnemyMaxHP         = data.MaxHP;
            _refParams.EnemyAtk           = data.Atk;
            _refParams.EnemyDef           = data.Def;
            _refParams.EnemyMoveRange     = data.MoveRange;
            _refParams.EnemyJumpForce     = data.JumpForce;
            _refParams.EnemyAtkRange      = data.AtkRange;
            _refParams.EnemyActGaugeMax   = data.ActGaugeMax;
            _refParams.EnemyActRecovery   = data.ActRecovery;
            _refParams.EnemyPrefab        = data.Prefab;
            _refParams.EnemyThinkType     = data.ThinkType;
            _refParams.EnemyInitGridIndex = data.InitGridIndex;
            _refParams.EnemyInitDir       = data.InitDir;
            return true;
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
            Vector3 targetPosition = _gridCursor.GetPosition() + offset;
            _mainCamera.transform.position = targetPosition;
            _mainCamera.transform.LookAt(_gridCursor.GetPosition());
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
                _gridCursor.Init( 0, _gridDirectionMoveCallbacks );  // グリッドカーソルの位置をタイル番号0の地点に合わせる

                return true;
            }

            return false;
        }
    }
} // namespace Frontier.DebugTools.StageEditor

#endif // UNITY_EDITOR