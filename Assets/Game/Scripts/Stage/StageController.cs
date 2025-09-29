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
        private Footprint _footprint;
        private List<GridMesh> _gridMeshs;
        private List<int> _attackableTileIndexs;

        void Awake()
        {
            TileMaterialLibrary.Init(); // タイルマテリアルの初期化

            if( null == _stageFileLoader )
            {
                _stageFileLoader = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StageFileLoader>( stageFileLoaderPrefab, true, false, "StageFileLoader" );
                NullCheck.AssertNotNull( _stageFileLoader, nameof( _stageFileLoader ) );
            }

            _gridMeshs              = new List<GridMesh>();
            _attackableTileIndexs   = new List<int>();
            _gridCursorCtrl         = CreateCursor();
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
        /// グリッド情報を更新します
        /// </summary>
        public void UpdateTileInfo()
        {
            // 一度全てのグリッド情報を元に戻す
            ResetGridInfo();
            // キャラクターが存在するグリッドの情報を更新
            TileBitFlag[] flags =
            {
                TileBitFlag.ALLY_EXIST,
                TileBitFlag.ENEMY_EXIST,
                TileBitFlag.OTHER_EXIST
            };

            for( int i = 0; i < ( int ) CHARACTER_TAG.NUM; ++i )
            {
                foreach( var chara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( ( CHARACTER_TAG ) i ) )
                {
                    var gridIndex = chara.Params.TmpParam.GetCurrentGridIndex();
                    ref var tileInfo = ref _stageDataProvider.CurrentData.GetTileInfo( gridIndex );
                    tileInfo.charaTag = chara.Params.CharacterParam.characterTag;
                    tileInfo.charaIndex = chara.Params.CharacterParam.characterIndex;
                    Methods.SetBitFlag( ref tileInfo.flag, flags[i] );
                }
            }
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
                ( MeshType meshType, bool condition )[] meshTypeAndConditions = new ( MeshType, bool )[ (int) MeshType.NUM ]
                {
                    ( MeshType.ATTACKABLE_TARGET_EXIST, Methods.CheckBitFlag(info.flag, TileBitFlag.ATTACKABLE_TARGET_EXIST) ),
                    ( MeshType.REACHABLE_ATTACK,        Methods.CheckBitFlag(info.flag, TileBitFlag.REACHABLE_ATTACK) ),
                    ( MeshType.MOVE,                    (0 <= info.estimatedMoveRange) ),
                    // 攻撃可能なタイルは移動可能タイルとほぼ重複するため、優先度が最も低くなるようにする
                    ( MeshType.ATTACKABLE,              Methods.CheckBitFlag(info.flag, TileBitFlag.ATTACKABLE) ),
                };

                for( int j = 0; j < ( int ) MeshType.NUM; ++j )
                {
                    if( meshTypeAndConditions[j].condition )
                    {
                        var gridMesh = _hierarchyBld.CreateComponentAndOrganize<GridMesh>( _gridMeshObject, true );
                        NullCheck.AssertNotNull( gridMesh, nameof( gridMesh ) );
                        if( gridMesh == null ) { continue; }

                        _gridMeshs.Add( gridMesh );
                        _gridMeshs[count++].DrawTileMesh( info.charaStandPos, TILE_SIZE, ( MeshType )Enum.ToObject( typeof( MeshType ), meshTypeAndConditions[j].meshType ) );

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 全てのタイルにおいて、指定のビットフラグの設定を解除します
        /// </summary>
        public void UnsetAllTilesBitFlag( TileBitFlag value )
        {
            // 全てのグリッドの移動・攻撃可否情報を初期化
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                Methods.UnsetBitFlag( ref _stageDataProvider.CurrentData.GetTileInfo( i ).flag, value );
            }
        }

        /// <summary>
        /// 全てのグリッドメッシュの描画を消去します
        /// </summary>
        public void ClearGridMeshDraw()
        {
            foreach( var grid in _gridMeshs )
            {
                grid.ClearDraw();
                grid.Remove();
            }
            _gridMeshs.Clear();
        }

        /// <summary>
        /// 攻撃可能情報を消去します
        /// </summary>
        public void ClearAttackableInformation()
        {
            _attackableTileIndexs.Clear();
            UnsetAllTilesBitFlag( TileBitFlag.REACHABLE_ATTACK | TileBitFlag.ATTACKABLE | TileBitFlag.ATTACKABLE_TARGET_EXIST );
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
        /// 選択グリッドのアクティブ状態を設定します
        /// </summary>
        /// <param name="isActive">設定するアクティブ状態</param>
        public void SetGridCursorControllerActive( bool isActive )
        {
            _gridCursorCtrl.SetActive( isActive );
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
        /// 現在選択しているグリッドの情報を取得します
        /// 攻撃対象選択状態では選択している攻撃対象が存在するグリッド情報を取得します
        /// </summary>
        /// <param name="gridInfo">該当するグリッドの情報</param>
        public void FetchCurrentGridInfo( out TileInformation gridInfo )
        {
            int index = 0;

            if( _gridCursorCtrl.GridState == GridCursorState.ATTACK )
            {
                index = _attackableTileIndexs[_gridCursorCtrl.GetAtkTargetIndex()];
            }
            else
            {
                index = _gridCursorCtrl.Index;
            }

            gridInfo = _stageDataProvider.CurrentData.GetTileInfo( index );
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
        /// タイルへの移動可能情報の登録を開始します
        /// </summary>
        /// <param name="dprtIndex"></param>
        /// <param name="moveRange"></param>
        /// <param name="atkRange"></param>
        /// <param name="jumpForce"></param>
        /// <param name="ownerIndex"></param>
        /// <param name="prevHeight"></param>
        /// <param name="ownerTileCosts"></param>
        /// <param name="selfTag"></param>
        /// <param name="isAttackable"></param>
        public void BeginRegisterMoveableTiles( int dprtIndex, int moveRange, int atkRange, int jumpForce, int ownerIndex, float dprtHeight, in int[] ownerTileCosts, CHARACTER_TAG selfTag, bool isAttackable )
        {
            Debug.Assert( dprtIndex.IsBetween( 0, _stageDataProvider.CurrentData.GetTileTotalNum() - 1 ), "StageController : Irregular Index." );

            var tileInfo = _stageDataProvider.CurrentData.GetTileInfo( dprtIndex );
            if( tileInfo == null ) { return; }
            tileInfo.estimatedMoveRange = moveRange;

            RegisterMoveableTilesAllSides( dprtIndex, moveRange, atkRange, jumpForce, ownerIndex, dprtHeight, in ownerTileCosts, selfTag, isAttackable );
        }

        /// <summary>
        /// タイルへの攻撃可能情報の登録を開始します
        /// </summary>
        /// <param name="dprtIndex">攻撃キャラクターが存在するグリッドのインデックス値</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="selfTag">攻撃を行うキャラクター自身のキャラクタータグ</param>
        public void BeginRegisterAttackableTiles( int dprtIndex, int atkRange, CHARACTER_TAG ownerTag, bool isClearAttackableInfo )
        {
            Debug.Assert( dprtIndex.IsBetween( 0, _stageDataProvider.CurrentData.GetTileTotalNum() - 1 ), "StageController : Irregular Index." );

            if( isClearAttackableInfo ) { ClearAttackableInformation(); }   // 全てのタイルの攻撃可否情報を初期化

            // 攻撃可否情報を各タイルに登録
            int targetTileIndex = dprtIndex;    // 開始時点では出発タイルと同じ
            RegisterAttackableTilesAllSides( dprtIndex, targetTileIndex, atkRange, ownerTag );
        }

        /// <summary>
        /// 攻撃可能タイルのうち、攻撃可能キャラクターが存在するタイルを専用のリストに追加していきます
        /// </summary>
        /// <param name="selfTag">攻撃を行うキャラクターのキャラクタータグ</param>
        /// <param name="target">予め攻撃対象が決まっている際に指定</param>
        /// <returns>攻撃可能キャラクターが存在している</returns>
        public bool CorrectAttackableTileIndexs( CHARACTER_TAG selfTag, Character target = null )
        {
            Character character = null;

            _gridCursorCtrl.ClearAtkTargetInfo();
            _attackableTileIndexs.Clear();

            // 攻撃可能、かつ攻撃対象となるキャラクターが存在するタイルのインデックス値をリストに登録
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                var info = _stageDataProvider.CurrentData.GetTileInfo( i );
                if( Methods.CheckBitFlag( info.flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    character = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable( info.charaTag, info.charaIndex );
                    if( character != null && character.Params.CharacterParam.characterTag != selfTag )
                    {
                        _attackableTileIndexs.Add( i );
                    }
                }
            }

            // グリッドカーソルの位置を、攻撃可能キャラクターが存在するタイル位置に設定
            if( 0 < _attackableTileIndexs.Count )
            {
                _gridCursorCtrl.SetAtkTargetNum( _attackableTileIndexs.Count );

                // 攻撃対象が定められている場合はその対象を探す
                if( target != null && 1 < _attackableTileIndexs.Count )
                {
                    for( int i = 0; i < _attackableTileIndexs.Count; ++i )
                    {
                        var info = GetTileInfo( _attackableTileIndexs[i] );
                        if( target == _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable( info.charaTag, info.charaIndex ) )
                        {
                            _gridCursorCtrl.SetAtkTargetIndex( i );
                            break;
                        }
                    }
                }
                // 定められていない場合は先頭を指定する
                else { _gridCursorCtrl.SetAtkTargetIndex( 0 ); }
            }

            return ( 0 < _attackableTileIndexs.Count );
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
        /// 2つの指定のインデックスが隣り合う座標に存在しているかを判定します
        /// </summary>
        /// <param name="fstIndex">指定インデックスその1</param>
        /// <param name="scdIndex">指定インデックスその2</param>
        /// <returns>隣り合うか否か</returns>
        public bool IsGridNextToEacheOther( int fstIndex, int scdIndex )
        {
            var colNum = _stageDataProvider.CurrentData.GridColumnNum;

            bool updown = ( Math.Abs( fstIndex - scdIndex ) == colNum );

            int fstQuotient     = fstIndex / colNum;
            int scdQuotient     = scdIndex / colNum;
            var fstRemainder    = fstIndex % colNum;
            var scdRemainder    = scdIndex % colNum;
            bool leftright      = ( fstQuotient == scdQuotient ) && ( Math.Abs( fstRemainder - scdRemainder ) == 1 );

            return updown || leftright;
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
        public int GetTotalTileNum()
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
        public List<WaypointInformation> ExtractShortestPath( int departGridIndex, int destGridIndex, int ownerJumpForce, in int[] ownerTileCosts, in List<int> candidateRouteIndexs )
        {
            if( departGridIndex == destGridIndex ) { return null; }

            ownerJumpForce = 1; // TODO : 動作確認用に仮で入れている。あとで必ず消すこと

            Dijkstra dijkstra   = new Dijkstra( candidateRouteIndexs.Count );
            StageData stageData = _stageDataProvider.CurrentData;
            int colNum          = stageData.GridColumnNum;

            // ジャンプ可能か否かを判定するラムダ式( 2つのタイルそれぞれの判定を一つの変数に纏めたいので、( bool, bool )の値に格納しています )
            Func<int, int, ( bool, bool )> canJumpOver = ( int a, int b ) =>
            {
                float diffHeight = stageData.GetTileData( b ).Height - stageData.GetTileData( a ).Height;
                return ( ( 0 < diffHeight ) ? (int)Mathf.Floor( diffHeight ) <= ownerJumpForce : true, ( 0 < -diffHeight ) ? (int)Mathf.Floor( -diffHeight ) <= ownerJumpForce : true );
            };

            // 出発グリッドからのインデックスの差を取得
            for( int i = 0; i + 1 < candidateRouteIndexs.Count; ++i )
            {
                for( int j = i + 1; j < candidateRouteIndexs.Count; ++j )
                {
                    if( !IsGridNextToEacheOther( candidateRouteIndexs[i], candidateRouteIndexs[j] ) ) { continue; } // 隣接していなければ次へ

                    (bool, bool) canJump = canJumpOver( candidateRouteIndexs[i], candidateRouteIndexs[j] ); // ジャンプ可能か否かを判定

                    // 移動可能な隣接グリッド情報をダイクストラに入れる
                    if( canJump.Item1 && canJump.Item2 )
                    {
                        dijkstra.Add( i, j, CalcurateTileCost( i, j, ownerJumpForce, in ownerTileCosts ) );
                        dijkstra.Add( j, i, CalcurateTileCost( j, i, ownerJumpForce, in ownerTileCosts ) );
                    }
                    else if( canJump.Item1 )
                    {
                        dijkstra.Add( i, j, CalcurateTileCost( i, j, ownerJumpForce, in ownerTileCosts ) );
                    }
                    else if( canJump.Item2 )
                    {
                        dijkstra.Add( j, i, CalcurateTileCost( j, i, ownerJumpForce, in ownerTileCosts ) );
                    }
                }
            }

            // ダイクストラから出発グリッドから目的グリッドまでの最短経路を得る
            return dijkstra.GetMinRoute( candidateRouteIndexs.IndexOf( departGridIndex ), candidateRouteIndexs.IndexOf( destGridIndex ), candidateRouteIndexs );
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
            FetchCurrentGridInfo( out info );
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
        /// _gridInfoの状態を基の状態に戻します
        /// </summary>
        private void ResetGridInfo()
        {
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                _stageDataProvider.CurrentData.TileDatas[i].ApplyBaseTileInfo();
            }
        }

        /// <summary>
        /// 指定のタイルから四方に向けて、移動可能なタイルを登録する処理を展開します
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <param name="moveRange"></param>
        /// <param name="atkRange"></param>
        /// <param name="jumpForce"></param>
        /// <param name="ownerIndex"></param>
        /// <param name="height"></param>
        /// <param name="ownerTileCosts"></param>
        /// <param name="selfTag"></param>
        /// <param name="isAttackable"></param>
        private void RegisterMoveableTilesAllSides( int tileIndex, int moveRange, int atkRange, int jumpForce, int ownerIndex, float height, in int[] ownerTileCosts, CHARACTER_TAG selfTag, bool isAttackable )
        {
            int colNum      = _stageDataProvider.CurrentData.GridColumnNum;

            // 左端を除外
            if( tileIndex % colNum != 0 )
            {
                RegisterMoveableTiles( tileIndex - 1, moveRange, atkRange, jumpForce, ownerIndex, height, in ownerTileCosts, selfTag, isAttackable );   // tileIndexからX軸方向へ-1
            }
            // 右端を除外
            if( ( tileIndex + 1 ) % colNum != 0 )
            {
                RegisterMoveableTiles( tileIndex + 1, moveRange, atkRange, jumpForce, ownerIndex, height, in ownerTileCosts, selfTag, isAttackable );   // tileIndexからX軸方向へ+1
            }
            // Z軸方向への加算と減算はそのまま
            RegisterMoveableTiles( tileIndex - colNum, moveRange, atkRange, jumpForce, ownerIndex, height, in ownerTileCosts, selfTag, isAttackable );  // tileIndexからZ軸方向へ-1
            RegisterMoveableTiles( tileIndex + colNum, moveRange, atkRange, jumpForce, ownerIndex, height, in ownerTileCosts, selfTag, isAttackable );  // tileIndexからZ軸方向へ+1
        }

        /// <summary>
        /// 指定のタイルから四方に向けて、攻撃可能なタイルを登録する処理を展開します
        /// </summary>
        /// <param name="dprtIndex">出発タイルインデックス</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="ownerTag">自身のキャラクタータグ</param>
        private void RegisterAttackableTilesAllSides( int dprtIndex, int targetTileIndex, int atkRange, CHARACTER_TAG ownerTag )
        {
            int colNum = _stageDataProvider.CurrentData.GridColumnNum;

            // 左端を除外
            if( targetTileIndex % colNum != 0 )
            {
                RegisterAttackableTiles( dprtIndex, targetTileIndex - 1, atkRange, ownerTag );  // targetTileIndexからX軸方向へ-1
            }
            // 右端を除外
            if( ( targetTileIndex + 1 ) % colNum != 0 )
            {
                RegisterAttackableTiles( dprtIndex, targetTileIndex + 1, atkRange, ownerTag );  // targetTileIndexからX軸方向へ+1
            }
            // Z軸方向への加算と減算はそのまま
            RegisterAttackableTiles( dprtIndex, targetTileIndex - colNum, atkRange, ownerTag ); // targetTileIndexからZ軸方向へ-1
            RegisterAttackableTiles( dprtIndex, targetTileIndex + colNum, atkRange, ownerTag ); // targetTileIndexからZ軸方向へ+1
        }

        /// <summary>
        /// 移動可能なグリッドを登録します
        /// </summary>
        /// <param name="tileIndex">登録対象のグリッドインデックス</param>
        /// <param name="moveRange">移動可能範囲値</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="jumpForce">ジャンプ値</param>
        /// <param name="ownerIndex">移動キャラクターのキャラクターインデックス</param>
        /// <param name="prevHeight">移動前のタイルの高さ</param>
        /// <param name="ownerTileCosts">各タイルの移動コスト(ステータス異常によって変化するためキャラ毎に個別)</param>
        /// <param name="selfTag">呼び出し元キャラクターのキャラクタータグ</param>
        /// <param name="isAttackable">呼び出し元のキャラクターが攻撃可能か否か</param>
        /// <param name="isDeparture">出発グリッドから呼び出されたか否か</param>
        private void RegisterMoveableTiles( int tileIndex, int moveRange, int atkRange, int jumpForce, int ownerIndex, float prevHeight, in int[] ownerTileCosts, CHARACTER_TAG selfTag, bool isAttackable )
        {
            var stageData = _stageDataProvider.CurrentData;
            int columnNum = stageData.GridColumnNum;

            // 範囲外のタイルは考慮しない
            if( tileIndex < 0 || stageData.GetTileTotalNum() <= tileIndex ) { return; }
            // 指定のタイル情報を取得
            var tileInfo = stageData.GetTileInfo( tileIndex );
            if( tileInfo == null ) { return; }
            // 移動不可のグリッドに辿り着いた場合は終了
            if( Methods.CheckBitFlag( tileInfo.flag, TileBitFlag.CANNOT_MOVE ) ) { return; }
            // 既に計算済みのグリッドであれば終了
            if( moveRange <= tileInfo.estimatedMoveRange ) { return; }
            // 自身に対する敵対勢力キャラクターが存在すれば終了
            TileBitFlag[] opponentTag = new TileBitFlag[( int ) CHARACTER_TAG.NUM]
            {
                TileBitFlag.ENEMY_EXIST | TileBitFlag.OTHER_EXIST,   // PLAYERにおける敵対勢力
                TileBitFlag.ALLY_EXIST | TileBitFlag.OTHER_EXIST,    // ENEMYにおける敵対勢力
                TileBitFlag.ALLY_EXIST | TileBitFlag.ENEMY_EXIST     // OTHERにおける敵対勢力
            };
            if( Methods.CheckBitFlag( tileInfo.flag, opponentTag[( int ) selfTag] ) ) { return; }

            // 直前のタイルとの高さの差分を求め、ジャンプ値と比較して移動可能かを判定する
            float curHeight     = stageData.TileDatas[tileIndex].Height;
            int heightCost      = CalcurateHeightCost( prevHeight, curHeight, jumpForce );

            // 現在グリッドの移動抵抗値を更新( 出発グリッドではmoveRangeの値をそのまま適応する )
            int tileTypeIndex           = Convert.ToInt32( stageData.TileDatas[tileIndex].Type );
            int currentMoveRange        =  moveRange - ownerTileCosts[tileTypeIndex] - heightCost;
            tileInfo.estimatedMoveRange = currentMoveRange;

            // 負の値であれば終了
            if( currentMoveRange < 0 ) { return; }
            // 攻撃範囲についても登録する
            if( isAttackable && ( tileInfo.charaTag == CHARACTER_TAG.NONE || tileInfo.charaIndex == ownerIndex ) )
            {
                BeginRegisterAttackableTiles( tileIndex, atkRange, selfTag, false );
            }

            RegisterMoveableTilesAllSides( tileIndex, currentMoveRange, atkRange, jumpForce, ownerIndex, curHeight, in ownerTileCosts, selfTag, isAttackable );
        }

        /// <summary>
        /// 攻撃可能なタイルを登録します
        /// </summary>
        /// <param name="dprtIndex">出発タイルインデックス</param>
        /// <param name="targetTileIndex">対象のグリッドインデックス</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="ownerTag">自身のキャラクタータグ</param>
        private void RegisterAttackableTiles( int dprtIndex, int targetTileIndex, int atkRange, CHARACTER_TAG ownerTag )
        {
            // 範囲外のグリッドは考慮しない
            var stageData = _stageDataProvider.CurrentData;
            if( !targetTileIndex.IsBetween( 0, stageData.GetTileTotalNum() - 1 ) ) { return; }
            // 移動不可のグリッドには攻撃できない
            if( Methods.CheckBitFlag( stageData.GetTileInfo( targetTileIndex ).flag, TileBitFlag.CANNOT_MOVE ) ) { return; }
            // 高低差が攻撃範囲を超過している場合は攻撃できない
            var dprtTileData    = stageData.GetTileData( dprtIndex );
            var targetTileData  = stageData.GetTileData( targetTileIndex );
            int diffHeight      = Convert.ToInt32( Mathf.Ceil( Mathf.Abs( targetTileData.Height - dprtTileData.Height ) ) );
            if( atkRange < diffHeight ) { return; }

            // 出発地点でなければ登録
            if( targetTileIndex != dprtIndex )
            {
                Methods.SetBitFlag( ref stageData.GetTileInfo( targetTileIndex ).flag, TileBitFlag.ATTACKABLE );
                var tileInfo = stageData.GetTileInfo( targetTileIndex );

                bool[] isMatch =
                {
                    (tileInfo.charaTag == CHARACTER_TAG.ENEMY || tileInfo.charaTag == CHARACTER_TAG.OTHER),     // PLAYER
                    (tileInfo.charaTag == CHARACTER_TAG.PLAYER || tileInfo.charaTag == CHARACTER_TAG.OTHER),    // ENEMY
                    (tileInfo.charaTag == CHARACTER_TAG.PLAYER || tileInfo.charaTag == CHARACTER_TAG.ENEMY)     // OTHER
                };

                if( isMatch[( int ) ownerTag] )
                {
                    Methods.SetBitFlag( ref stageData.GetTileInfo( dprtIndex ).flag, TileBitFlag.REACHABLE_ATTACK );
                    Methods.SetBitFlag( ref stageData.GetTileInfo( targetTileIndex ).flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
                }
            }

            if( --atkRange <= 0 ) { return; }   // 負の値であれば終了

            RegisterAttackableTilesAllSides( dprtIndex, targetTileIndex, atkRange, ownerTag );  // 現在のtargetTileIndexの地点から更に四方に展開
        }

        /// <summary>
        /// 隣接タイル間の移動コストを計算します
        /// </summary>
        /// <param name="dprtIndex"></param>
        /// <param name="destIndex"></param>
        /// <param name="jumpForce"></param>
        /// <param name="ownerTileCosts"></param>
        /// <returns></returns>
        private int CalcurateTileCost( int dprtIndex, int destIndex, int jumpForce, in int[] ownerTileCosts )
        {
            Debug.Assert( IsGridNextToEacheOther( dprtIndex, destIndex ), "" );

            // 目的地のタイルのタイプから移動コストを取得
            var destTileData        = _stageDataProvider.CurrentData.GetTileData( destIndex );
            TileType destTileType   = destTileData.Type;
            int tileCost            = ownerTileCosts[( int ) destTileType ];

            // 高低差コストを取得
            int heightCost = CalcurateHeightCost( dprtIndex, destIndex, jumpForce );

            return tileCost + heightCost;
        }

        /// <summary>
        /// 隣接タイル間の高低差コストを計算します
        /// </summary>
        /// <param name="dprtIndex"></param>
        /// <param name="destIndex"></param>
        /// <param name="jumpForce"></param>
        /// <returns></returns>
        private int CalcurateHeightCost( int dprtIndex, int destIndex, int jumpForce )
        {
            Debug.Assert( IsGridNextToEacheOther( dprtIndex, destIndex ), "" );

            float dprtHeight = _stageDataProvider.CurrentData.GetTileData( dprtIndex ).Height;
            float destHeight = _stageDataProvider.CurrentData.GetTileData( destIndex ).Height;

            return CalcurateHeightCost( dprtHeight, destHeight, jumpForce );
        }

        private int CalcurateHeightCost( float dprtHeight, float destHeight, int jumpForce )
        {
            int retCost = 0;
            float diffHeightCeil = Mathf.Ceil( destHeight - dprtHeight );

            if( jumpForce < diffHeightCeil ||                           // ジャンプ力を超過している場合は、移動不可
                diffHeightCeil <= -1 * ( jumpForce + DESCENT_MARGIN ) ) // 移動先のタイルが低くてても、高低差がジャンプ力+定数を超過している場合は、移動不可
            {
                retCost += short.MaxValue;  // int.MaxValueを入れるとオーバーフローしてしまうため、short.MaxValueに留める
            }
            // ジャンプ力以内の高さであれば、その分をコストに加算する
            else if( 0 < diffHeightCeil )
            {
                retCost += Convert.ToInt32( diffHeightCeil );
            }

            return retCost;
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