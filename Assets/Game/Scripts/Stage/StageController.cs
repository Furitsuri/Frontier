using Frontier.Registries;
using Frontier.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    public sealed class StageController
    {
        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private PrefabRegistry _prefabReg              = null;

        private GridCursorController _gridCursorCtrl;
        private StageFileLoader _stageFileLoader;
        private StageDirectionConverter _directionConverter;
        private TileDataHandler _tileDataHdlr;

		public TileDataHandler TileDataHdlr() => _tileDataHdlr;

        public void Setup()
        {
            TileMaterialLibrary.Init(); // タイルマテリアルの初期化

            LazyInject.GetOrCreate( ref _gridCursorCtrl, () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<GridCursorController>( _prefabReg.GridCursorCtrlPrefab, true, true, "GridCursorController" ) );
            LazyInject.GetOrCreate( ref _stageFileLoader, () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StageFileLoader>( _prefabReg.StageFileLoaderPrefab, true, false, "StageFileLoader" ) );
            LazyInject.GetOrCreate( ref _directionConverter, () => _hierarchyBld.InstantiateWithDiContainer<StageDirectionConverter>( false ) );
            LazyInject.GetOrCreate( ref _tileDataHdlr, () => _hierarchyBld.InstantiateWithDiContainer<TileDataHandler>( false ) );
        }

        #region PUBLIC_METHOD

        /// <summary>
        /// 初期化を行います
        /// </summary>
        public void Init( BattleCameraController btlCameraCtrl )
        {
            _stageFileLoader.Init( _prefabReg.TilePrefabs );
            _stageFileLoader.Load( 0 );
            _gridCursorCtrl.Init( 0 );
            _tileDataHdlr.Init();
            _directionConverter.Regist( btlCameraCtrl );

            // 攻撃可能タイルのインデックスリストをグリッドカーソルコントローラーに渡す
            _gridCursorCtrl.AssignAttackableTileIndices( _tileDataHdlr.AttackableTileIndices.AsReadOnly() );
        }

        /// <summary>
        /// 選択グリッドを指定のキャラクターのタイルに合わせます
        /// </summary>
        /// <param name="character">指定キャラクター</param>
        public void ApplyCurrentGrid2CharacterTile( Character character )
        {
            _gridCursorCtrl.SetTileIndex( character.BattleParams.TmpParam.CurrentTileIndex );
        }

        /// <summary>
        /// グリッドカーソルにキャラクターをバインドします
        /// </summary>
        /// <param name="state">バインドタイプ</param>
        /// <param name="bindCharacter">バインド対象のキャラクター</param>
        public void BindToGridCursor( GridCursorState state, Character character )
        {
            _gridCursorCtrl.GridState       = state;
            _gridCursorCtrl.BindCharacter   = character;
        }

        /// <summary>
        /// グリッドカーソルのキャラクターバインドを解除します
        /// </summary>
        public void ClearGridCursorBind()
        {
            if( _gridCursorCtrl.BindCharacter != null )
            {
                _gridCursorCtrl.BindCharacter.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_CHARACTER );
            }

            _gridCursorCtrl.GridState = GridCursorState.NONE;
            _gridCursorCtrl.BindCharacter = null;
        }

        /// <summary>
        /// 選択グリッドのアクティブ状態を設定します
        /// </summary>
        /// <param name="isActive">設定するアクティブ状態</param>
        public void SetActiveGridCursor( bool isActive )
        {
            _gridCursorCtrl.SetActive( isActive );
        }

        /// <summary>
        /// グリッドカーソルの位置を、攻撃可能キャラクターが存在するタイル位置に設定します
        /// </summary>
        /// <param name="designatedTarget"></param>
        public void MoveGridCursorToAttackableTile( Character designatedTarget = null )
        {
            var attackableTileIndices = _tileDataHdlr.AttackableTileIndices;
            if( attackableTileIndices.Count <= 0 ) { return; }

            // 攻撃対象引数targetが定められている場合はその対象を探す
            if( designatedTarget != null && 1 < attackableTileIndices.Count )
            {
                for( int i = 0; i < attackableTileIndices.Count; ++i )
                {
                    var tileData = _stageDataProvider.CurrentData.GetTile( attackableTileIndices[i] ).DynamicData();
                    if( designatedTarget.CharaKey() == tileData.CharaKey )
                    {
                        _gridCursorCtrl.SetAtkTargetIndex( i );
                        break;
                    }
                }
            }
            // 定められていない場合は先頭を指定する
            else { _gridCursorCtrl.SetAtkTargetIndex( 0 ); }
        }

        public bool OperateGridCursorController( Direction direction )
        {
            if( direction == Direction.NONE ) { return false; }

            _gridCursorCtrl.Move( direction );

            return true;
        }

        /// <summary>
        /// 指定方向にグリッドを移動させます
        /// </summary>
        /// <param name="direction">グリッドの移動方向</param>
        /// /// <returns>グリッド移動の有無</returns>
        public bool OperateGridCursorControllerBasedOnCamera( ref Direction direction )
        {
            if( direction == Direction.NONE ) { return false; }

            direction = ConvertDirectionDependOnCameraAngle( direction );

            _gridCursorCtrl.Move( direction );

            return true;
        }

        /// <summary>
        /// 攻撃対象を設定します
        /// </summary>
        /// <param name="direction">グリッドの移動方向</param>
        /// <returns>グリッド移動の有無</returns>
        public bool OperateTargetSelect( Direction direction )
        {
            if( Direction.NONE == direction ) { return false; }

            _gridCursorCtrl.TransitAttackTarget( direction );

            return true;
        }

        /// <summary>
        /// グリッドカーソルのインデックス値を取得します
        /// </summary>
        /// <returns>現在の選択グリッドのインデックス値</returns>
        public int GetCurrentGridIndex()
        {
            return _gridCursorCtrl.Index;
        }

        /// <summary>
        /// ステージ上の全てのタイルの数を取得します
        /// </summary>
        /// <returns>全てのタイルの数</returns>
        public int GetTileTotalNum()
        {
            return _stageDataProvider.CurrentData.GetTileTotalNum();
        }

        /// <summary>
        /// 2つのグリッド間の総レンジ数を求めます
        /// </summary>
        /// <param name="gridIdxA"></param>
        /// <param name="gridIdxB"></param>
        /// <returns>タイルのrowとcolumnにおけるレンジ差の合計</returns>
        public int CalcurateTotalRange( int gridIdxA, int gridIdxB )
        {
            (int, int) ranges = CalcurateRanges( gridIdxA, gridIdxB );

            return ranges.Item1 + ranges.Item2;
        }

        /// <summary>
        /// 縦軸と横軸のグリッド数を取得します
        /// </summary>
        /// <returns>縦軸と横軸のグリッド数</returns>
        public (int, int) GetGridNumsXZ()
        {
            return (_stageDataProvider.CurrentData.TileRowNum, _stageDataProvider.CurrentData.TileColNum);
        }

        /// <summary>
        /// 2つのグリッド間のグリッドレンジを求めます
        /// </summary>
        /// <param name="gridIdxA"></param>
        /// <param name="gridIdxB"></param>
        /// <returns>タイルのrowとcolumnにおけるレンジ差</returns>
        public (int, int) CalcurateRanges( int gridIdxA, int gridIdxB )
        {
            int range = Math.Abs( gridIdxA - gridIdxB );
            int colNum = _stageDataProvider.CurrentData.TileColNum;

            return (range % colNum, range / colNum);
        }

        /// <summary>
        /// ステージの中心位置を取得します
        /// </summary>
        /// <returns>ステージの中心位置</returns>
        public Vector3 GetCentralPos()
        {
            Vector3 retCentralPos = new Vector3( 0.5f * _stageDataProvider.CurrentData.WidthX(), 0f, 0.5f * _stageDataProvider.CurrentData.WidthZ() );

            return retCentralPos;
        }

        public Vector3 GetCurrentGridPosition()
        {
            TileStaticData tileData = _stageDataProvider.CurrentData.GetTileStaticData( _gridCursorCtrl.Index );
            return tileData.CharaStandPos;
        }

        public Direction ConvertDirectionDependOnCameraAngle( Direction dir )
        {
            return _directionConverter.ConvertDirectionDependOnCameraAngle( dir );
        }

        /// <summary>
        /// グリッドカーソルの状態を取得します
        /// </summary>
        /// <returns>現在の選択グリッドの状態</returns>
        public GridCursorState GetGridCursorControllerState()
        {
            return _gridCursorCtrl.GridState;
        }

        /// <summary>
        /// グリッドカーソルがバインドしているキャラクターを取得します
        /// </summary>
        /// <returns>バインドしているキャラクター(存在しない場合はnull)</returns>
        public Character GetBindCharacterFromGridCursor()
        {
            return _gridCursorCtrl.BindCharacter;
        }

        public TileStaticData GetTileStaticData( int index )
        {
            return _stageDataProvider.CurrentData.GetTile( index ).StaticData();
        }

        public TileDynamicData GetTileDynamicData( int index )
        {
            return _stageDataProvider.CurrentData.GetTile( index ).DynamicData();
        }

        /// <summary>
        /// 出発地点と目的地から移動経路となるグリッドのインデックスリストを取得します
        /// </summary>
        /// <param name="departGridIndex">出発地グリッドのインデックス</param>
        /// <param name="destGridIndex">目的地グリッドのインデックス</param>
        public List<WaypointInformation> ExtractShortestPath( int departGridIndex, int destGridIndex, int ownerJumpForce, in int[] ownerTileCosts, Dictionary<int, TileDynamicData> movaableTileMap )
        {
            if( departGridIndex == destGridIndex ) { return null; } // 出発地と目的地が同じ場合は経路なし

            List<int> candidataPathIndices = movaableTileMap.Keys.ToList();
            Dijkstra dijkstra   = new Dijkstra( candidataPathIndices.Count );
            StageData stageData = _stageDataProvider.CurrentData;
            int colNum          = stageData.TileColNum;

            // 出発グリッドからのインデックスの差を取得
            for( int i = 0; i + 1 < candidataPathIndices.Count; ++i )
            {
                for( int j = i + 1; j < candidataPathIndices.Count; ++j )
                {
                    (int i2jCost, bool passablei2j ) = _tileDataHdlr.CalcurateTileCost( candidataPathIndices[i], candidataPathIndices[j], ownerJumpForce, in ownerTileCosts );
                    (int j2iCost, bool passablej2i ) = _tileDataHdlr.CalcurateTileCost( candidataPathIndices[j], candidataPathIndices[i], ownerJumpForce, in ownerTileCosts );

                    // 移動可能な隣接タイル情報をダイクストラに入れる
                    if( passablei2j )
                    {
                        dijkstra.Add( i, j, i2jCost );
                    }
                    if( passablej2i )
                    {
                        dijkstra.Add( j, i, j2iCost );
                    }
                }
            }

            // ダイクストラから出発グリッドから目的グリッドまでの最短経路を得る
            return dijkstra.GetMinRoute( candidataPathIndices.IndexOf( departGridIndex ), candidataPathIndices.IndexOf( destGridIndex ), candidataPathIndices );
        }

        /// <summary>
        /// 指定されたインデックス間のグリッド長を返します
        /// </summary>
        /// <param name="fromIndex">始点インデックス</param>
        /// <param name="toIndex">終点インデックス</param>
        /// <returns>グリッド長</returns>
        public float CalcurateGridLength( int fromIndex, int toIndex )
        {
            var from        = _stageDataProvider.CurrentData.GetTileStaticData( fromIndex ).CharaStandPos;
            var to          = _stageDataProvider.CurrentData.GetTileStaticData( toIndex ).CharaStandPos;
            var gridLength  = ( from - to ).magnitude / TILE_SIZE;

            return gridLength;
        }

        #endregion // PUBLIC_METHOD

        #region PRIVATE_METHOD

        /// <summary>
        /// 頂点配列データをすべて指定の方向へ回転移動させます
        /// </summary>
        /// <param name="vertices">回転させる頂点配列データ</param>
        /// <param name="rotDirection">回転方向</param>
        /// <returns>回転させた頂点配列データ</returns>
        Vector3[] RotationVertices( Vector3[] vertices, Vector3 rotDirection )
        {
            Vector3[] ret = new Vector3[vertices.Length];
            for( int i = 0; i < vertices.Length; i++ )
            {
                ret[i] = Quaternion.LookRotation( rotDirection ) * vertices[i];
            }
            return ret;
        }

        #endregion // PRIVATE_METHOD

        /*
#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(StageController))]
        public class StageControllerEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                StageController script = target as StageController;

                // ステージ情報からサイズを決める際はサイズ編集を不可にする
                EditorGUI.BeginDisabledGroup(script.isAdjustStageScale);
                script._stageDataProvider.CurrentData.SetTileRowNum( EditorGUILayout.IntField("X方向グリッド数", script._stageDataProvider.CurrentData.TileRowNum) );
                script._stageDataProvider.CurrentData.SetTileColNum( EditorGUILayout.IntField("Z方向グリッド数", script._stageDataProvider.CurrentData.TileColNum) );
                EditorGUI.EndDisabledGroup();

                base.OnInspectorGUI();
            }
        }
#endif // UNITY_EDITOR
        */
    }
}