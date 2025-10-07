using Frontier.Battle;
using Frontier.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Xsl;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    [RequireComponent( typeof( MeshFilter ), typeof( MeshRenderer ) )]
    public sealed class StageController : Controller
    {
        /// <summary>
        /// キャラクターの位置を元に戻す際に使用します
        /// </summary>
        public struct Footprint
        {
            public int gridIndex;
            public Quaternion rotation;
        }

        [Header( "デフォルトで読込むステージのインデックス値" )]
        [SerializeField] private int deafultLoadStageIndex;

        [Header( "Prefabs" )]
        [SerializeField] private GameObject stageFileLoaderPrefab;
        [SerializeField] private GameObject _gridMeshObject;
        [SerializeField] private GameObject _gridCursorCtrlObject;
        [SerializeField] private GameObject[] _tilePrefabs;

        [SerializeField] public float BattlePosLengthFromCentral { get; private set; } = 2.0f;

        [Inject] private IStageDataProvider _stageDataProvider = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        public bool back = true;
        private BattleRoutineController _btlRtnCtrl;
        private GridCursorController _gridCursorCtrl;
        private StageFileLoader _stageFileLoader;
        private TileInfoDataHandler _tileInfoDataHdlr;
        private Footprint _footprint;
        private List<GridMesh> _tileMeshes;

        public TileInfoDataHandler TileInfoDataHdlr() => _tileInfoDataHdlr;

        void Awake()
        {
            TileMaterialLibrary.Init(); // タイルマテリアルの初期化

            _gridCursorCtrl = CreateCursor();

            if( null == _stageFileLoader )
            {
                _stageFileLoader = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StageFileLoader>( stageFileLoaderPrefab, true, false, "StageFileLoader" );
                NullCheck.AssertNotNull( _stageFileLoader, nameof( _stageFileLoader ) );
            }

            if( null == _tileInfoDataHdlr )
            {
                _tileInfoDataHdlr = _hierarchyBld.InstantiateWithDiContainer<TileInfoDataHandler>( false );
                NullCheck.AssertNotNull( _tileInfoDataHdlr, nameof( _tileInfoDataHdlr ) );
            }

            _tileMeshes              = new List<GridMesh>();
        }

        #region PUBLIC_METHOD

        /// <summary>
        /// 初期化を行います
        /// </summary>
        /// <param name="btlRtnCtrl">バトルマネージャ</param>
        public void Init( BattleRoutineController btlRtnCtrl )
        {
            _btlRtnCtrl = btlRtnCtrl;

            _stageFileLoader.Init( _tilePrefabs );
            _stageFileLoader.Load( deafultLoadStageIndex );
        }

        /// <summary>
        /// 全てのタイル情報を一時保存します
        /// </summary>
        public void HoldAllTileInfo()
        {
            for( int i = 0; i < _stageDataProvider.CurrentData.TileDatas.Length; ++i )
            {
                _stageDataProvider.CurrentData.GetTileData( i ).HoldCurrentTileInfo();
            }
        }

        /// <summary>
        /// 全てのタイルに対し、一時保存中のタイル情報を、現在のタイル情報に適応させます
        /// </summary>
        public void ApplyAllTileInfoFromHeld()
        {
            for( int i = 0; i < _stageDataProvider.CurrentData.TileDatas.Length; ++i )
            {
                _stageDataProvider.CurrentData.GetTileData( i ).ApplyHeldTileInfo();
            }
        }

        /// <summary>
        /// 選択グリッドを指定のキャラクターのグリッドに合わせます
        /// </summary>
        /// <param name="character">指定キャラクター</param>
        public void ApplyCurrentGrid2CharacterGrid( Character character )
        {
            _gridCursorCtrl.Index = character.Params.TmpParam.GetCurrentGridIndex();
        }

        /// <summary>
        /// 全てのタイルの情報メッシュを描画します
        /// </summary>
        public void DrawAllTileInformationMeshes()
        {
            int count = 0;

            // グリッドの状態をメッシュで描画
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                var info = _stageDataProvider.CurrentData.GetTileInfo( i );

                // メッシュタイプとそれに対応する描画条件( MEMO : 描画優先度の高い順に並べること )
                ( MeshType meshType, bool condition )[] meshTypeAndConditions = new ( MeshType, bool )[]
                {
                    ( MeshType.ATTACKABLE_TARGET_EXIST, Methods.CheckBitFlag(info.flag, TileBitFlag.ATTACKABLE_TARGET_EXIST) ),
                    ( MeshType.REACHABLE_ATTACK,        Methods.CheckBitFlag(info.flag, TileBitFlag.REACHABLE_ATTACK) ),
                    ( MeshType.MOVE,                    (0 <= info.estimatedMoveRange) ),
                    // 攻撃可能なタイルは移動可能タイルとほぼ重複するため、優先度が最も低くなるようにする
                    ( MeshType.ATTACKABLE,              Methods.CheckBitFlag(info.flag, TileBitFlag.ATTACKABLE) ),
                };

                for( int j = 0; j < meshTypeAndConditions.Length; ++j )
                {
                    if( meshTypeAndConditions[j].condition )
                    {
                        var gridMesh = _hierarchyBld.CreateComponentAndOrganize<GridMesh>( _gridMeshObject, true );
                        NullCheck.AssertNotNull( gridMesh, nameof( gridMesh ) );

                        _tileMeshes.Add( gridMesh );
                        _tileMeshes[count++].DrawTileMesh( info.charaStandPos, TILE_SIZE, TileColors.Colors[( int )  meshTypeAndConditions[j].meshType ] );

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 全てのタイルメッシュの描画を消去します
        /// </summary>
        public void ClearTileMeshDraw()
        {
            foreach( var grid in _tileMeshes )
            {
                grid.ClearDraw();
                grid.Remove();
            }
            _tileMeshes.Clear();
        }

        /// <summary>
        /// グリッドカーソルにキャラクターをバインドします
        /// </summary>
        /// <param name="state">バインドタイプ</param>
        /// <param name="bindCharacter">バインド対象のキャラクター</param>
        public void BindToGridCursor( GridCursorState state, Character character )
        {
            _gridCursorCtrl.GridState = state;
            _gridCursorCtrl.BindCharacter = character;
        }

        /// <summary>
        /// グリッドカーソルのキャラクターバインドを解除します
        /// </summary>
        public void ClearGridCursroBind()
        {
            if( _gridCursorCtrl.BindCharacter != null )
            {
                _gridCursorCtrl.BindCharacter.gameObject.SetLayerRecursively( LayerMask.NameToLayer( Constants.LAYER_NAME_CHARACTER ) );
            }

            _gridCursorCtrl.GridState = GridCursorState.NONE;
            _gridCursorCtrl.BindCharacter = null;
        }

        /// <summary>
        /// 選択グリッドのアクティブ状態を設定します
        /// </summary>
        /// <param name="isActive">設定するアクティブ状態</param>
        public void SetGridCursorControllerActive( bool isActive )
        {
            _gridCursorCtrl.SetActive( isActive );
        }

        /// <summary>
        /// グリッドのメッシュの描画の切替を行います
        /// </summary>
        /// <param name="isDisplay">描画するか否か</param>
        public void ToggleMeshDisplay( bool isDisplay )
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if( meshRenderer != null )
            {
                meshRenderer.enabled = isDisplay;
            }
        }

        /// <summary>
        /// 指定方向にグリッドを移動させます
        /// </summary>
        /// <param name="direction">グリッドの移動方向</param>
        /// /// <returns>グリッド移動の有無</returns>
        public bool OperateGridCursorController( Constants.Direction direction )
        {
            if( direction == Constants.Direction.FORWARD )  { _gridCursorCtrl.Up();     return true; }
            if( direction == Constants.Direction.BACK )     { _gridCursorCtrl.Down();   return true; }
            if( direction == Constants.Direction.LEFT )     { _gridCursorCtrl.Left();   return true; }
            if( direction == Constants.Direction.RIGHT )    { _gridCursorCtrl.Right();  return true; }

            return false;
        }

        /// <summary>
        /// 攻撃対象を設定します
        /// </summary>
        /// <param name="direction">グリッドの移動方向</param>
        /// <returns>グリッド移動の有無</returns>
        public bool OperateTargetSelect( Constants.Direction direction )
        {
            if( direction == Constants.Direction.FORWARD || direction == Constants.Direction.LEFT ) { _gridCursorCtrl.TransitPrevTarget(); return true; }
            if( direction == Constants.Direction.BACK || direction == Constants.Direction.RIGHT ) { _gridCursorCtrl.TransitNextTarget(); return true; }

            return false;
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
        /// 攻撃可能な標的の数を取得します
        /// </summary>
        /// <returns>標的の数</returns>
        public int GetAttackabkeTargetNum()
        {
            return _gridCursorCtrl.GetAttackableTargetNum();
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
            return (_stageDataProvider.CurrentData.GridRowNum, _stageDataProvider.CurrentData.GridColumnNum);
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
            int colNum = _stageDataProvider.CurrentData.GridColumnNum;

            return (range % colNum, range / colNum);
        }

        /// <summary>
        /// ステージの中心位置を取得します
        /// </summary>
        /// <returns>ステージの中心位置</returns>
        public Vector3 GetCentralPos()
        {
            Vector3 retCentralPos = transform.position + new Vector3( 0.5f * _stageDataProvider.CurrentData.WidthX(), 0f, 0.5f * _stageDataProvider.CurrentData.WidthZ() );

            return retCentralPos;
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

        public StageTileData GetTileData( int index )
        {
            return _stageDataProvider.CurrentData.GetTileData( index );
        }

        /// <summary>
        /// 指定インデックスのグリッド情報を取得します
        /// </summary>
        /// <param name="index">指定するインデックス値</param>
        /// <returns>指定インデックスのグリッド情報</returns>
        public ref TileInformation GetTileInfo( int index )
        {
            return ref _stageDataProvider.CurrentData.GetTileData( index ).GetTileInfo();
        }

        /// <summary>
        /// 出発地点と目的地から移動経路となるグリッドのインデックスリストを取得します
        /// </summary>
        /// <param name="departGridIndex">出発地グリッドのインデックス</param>
        /// <param name="destGridIndex">目的地グリッドのインデックス</param>
        public List<WaypointInformation> ExtractShortestPath( int departGridIndex, int destGridIndex, int ownerJumpForce, in int[] ownerTileCosts, in List<int> candidatePathIndexs )
        {
            if( departGridIndex == destGridIndex ) { return null; } // 出発地と目的地が同じ場合は経路なし

            Dijkstra dijkstra   = new Dijkstra( candidatePathIndexs.Count );
            StageData stageData = _stageDataProvider.CurrentData;
            int colNum          = stageData.GridColumnNum;

            // 出発グリッドからのインデックスの差を取得
            for( int i = 0; i + 1 < candidatePathIndexs.Count; ++i )
            {
                for( int j = i + 1; j < candidatePathIndexs.Count; ++j )
                {
                    (int i2jCost, bool passablei2j ) = _tileInfoDataHdlr.CalcurateTileCost( candidatePathIndexs[i], candidatePathIndexs[j], ownerJumpForce, in ownerTileCosts );
                    (int j2iCost, bool passablej2i ) = _tileInfoDataHdlr.CalcurateTileCost( candidatePathIndexs[j], candidatePathIndexs[i], ownerJumpForce, in ownerTileCosts );

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
            return dijkstra.GetMinRoute( candidatePathIndexs.IndexOf( departGridIndex ), candidatePathIndexs.IndexOf( destGridIndex ), candidatePathIndexs );
        }

        /// <summary>
        /// キャラクターの位置及び向きを保持します
        /// </summary>
        /// <param name="footprint">保持する値</param>
        public void HoldFootprint( Character chara )
        {
            _footprint.gridIndex = chara.Params.TmpParam.gridIndex;
            _footprint.rotation = chara.transform.rotation;
        }

        /// <summary>
        /// 保持していた位置及び向きを指定のキャラクターに設定します
        /// </summary>
        /// <param name="character">指定するキャラクター</param>
        public void FollowFootprint( Character character )
        {
            _gridCursorCtrl.Index = _footprint.gridIndex;
            character.Params.TmpParam.SetCurrentGridIndex( _footprint.gridIndex );
            TileInformation info;
            _tileInfoDataHdlr.FetchCurrentTileInfo( out info );
            character.transform.position = info.charaStandPos;
            character.transform.rotation = _footprint.rotation;
        }

        /// <summary>
        /// 指定されたインデックス間のグリッド長を返します
        /// </summary>
        /// <param name="fromIndex">始点インデックス</param>
        /// <param name="toIndex">終点インデックス</param>
        /// <returns>グリッド長</returns>
        public float CalcurateGridLength( int fromIndex, int toIndex )
        {
            var from = _stageDataProvider.CurrentData.GetTileInfo( fromIndex ).charaStandPos;
            var to = _stageDataProvider.CurrentData.GetTileInfo( toIndex ).charaStandPos;
            var gridLength = ( from - to ).magnitude / TILE_SIZE;

            return gridLength;
        }

        #endregion // PUBLIC_METHOD

        #region PRIVATE_METHOD

        /// <summary>
        /// グリッドカーソルを作成します
        /// </summary>
        /// <returns>作成したグリッドカーソル</returns>
        private GridCursorController CreateCursor()
        {
            GridCursorController gridCursorCtrl = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<GridCursorController>( _gridCursorCtrlObject, true, true, "GridCursorController" );
            NullCheck.AssertNotNull( gridCursorCtrl, nameof( gridCursorCtrl ) );
            gridCursorCtrl.Init( 0 );

            return gridCursorCtrl;
        }

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
            override public void OnInspectorGUI()
            {
                StageController script = target as StageController;

                // ステージ情報からサイズを決める際はサイズ編集を不可にする
                EditorGUI.BeginDisabledGroup(script.isAdjustStageScale);
                script._stageDataProvider.CurrentData.SetGridRowNum( EditorGUILayout.IntField("X方向グリッド数", script._stageDataProvider.CurrentData.GridRowNum) );
                script._stageDataProvider.CurrentData.SetGridColumnNum( EditorGUILayout.IntField("Z方向グリッド数", script._stageDataProvider.CurrentData.GridColumnNum) );
                EditorGUI.EndDisabledGroup();

                base.OnInspectorGUI();
            }
        }
#endif // UNITY_EDITOR
        */
    }
}