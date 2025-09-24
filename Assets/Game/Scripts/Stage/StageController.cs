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
        private List<int> _attackableGridIndexs;

        void Awake()
        {
            TileMaterialLibrary.Init(); // タイルマテリアルの初期化

            if( null == _stageFileLoader )
            {
                _stageFileLoader = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StageFileLoader>( stageFileLoaderPrefab, true, false, "StageFileLoader" );
                NullCheck.AssertNotNull( _stageFileLoader, nameof( _stageFileLoader ) );
            }

            _gridMeshs = new List<GridMesh>();
            _attackableGridIndexs = new List<int>();

            _gridCursorCtrl = CreateCursor();
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
        public void UpdateGridInfo()
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
        /// グリッドに移動可能情報を登録します
        /// </summary>
        /// <param name="departIndex">移動キャラクターが存在するグリッドのインデックス値</param>
        /// <param name="moveRange">移動可能範囲値</param>
        /// <param name="attackRange">攻撃可能範囲値</param>
        /// <param name="selfTag">キャラクタータグ</param>
        /// <param name="isAttackable">攻撃可能か否か</param>
        public void RegistMoveableInfo( int departIndex, int moveRange, int attackRange, int jumpForce, int selfCharaIndex, float currHeight, CHARACTER_TAG selfTag, bool isAttackable )
        {
            Debug.Assert( departIndex.IsBetween( 0, _stageDataProvider.CurrentData.GetTileTotalNum() - 1 ), "StageController : Irregular Index." );

            // 移動可否情報を各グリッドに登録
            RegistMoveableEachGrid( departIndex, moveRange, attackRange, jumpForce, selfCharaIndex, currHeight, selfTag, isAttackable, true );
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
        /// 攻撃可能なキャラクターが存在するグリッドにグリッドカーソルの位置を設定します
        /// </summary>
        /// <param name="target">予め攻撃対象が決まっている際に指定</param>
        public void SetupGridCursorControllerToAttackCandidate( Character target = null )
        {
            // 選択グリッドを自動的に攻撃可能キャラクターの存在するグリッドインデックスに設定
            if( 0 < _attackableGridIndexs.Count )
            {
                _gridCursorCtrl.SetAtkTargetNum( _attackableGridIndexs.Count );

                // 攻撃対象が既に決まっている場合は対象を探す
                if( target != null && 1 < _attackableGridIndexs.Count )
                {
                    for( int i = 0; i < _attackableGridIndexs.Count; ++i )
                    {
                        var info = GetTileInfo( _attackableGridIndexs[i] );

                        Character chara = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable( info.charaTag, info.charaIndex );

                        if( target == chara )
                        {
                            _gridCursorCtrl.SetAtkTargetIndex( i );
                            break;
                        }
                    }
                }
                else
                {
                    _gridCursorCtrl.SetAtkTargetIndex( 0 );
                }
            }
        }

        /// <summary>
        /// 移動可能グリッドを描画します
        /// </summary>
        /// <param name="departIndex">移動キャラクターが存在するグリッドのインデックス値</param>
        /// <param name="moveableRange">移動可能範囲値</param>
        /// <param name="attackableRange">攻撃可能範囲値</param>
        public void DrawMoveableGrids( int departIndex, int moveableRange, int attackableRange )
        {
            Debug.Assert( 0 <= departIndex && departIndex < _stageDataProvider.CurrentData.GetTileTotalNum(), "StageController : Irregular Index." );

            int count = 0;

            // 3つの条件毎に異なるメッシュタイプやデバッグ表示があるため、
            // for文で判定するためにそれぞれを配列化
            MeshType[] meshTypes =
            {
                MeshType.ATTACKABLE_TARGET_EXIST,
                MeshType.ATTACKABLE,
                MeshType.MOVE,
                MeshType.REACHABLE_ATTACK
            };

            string[] dbgStrs =
            {
                "Attackable Target Exist Grid Index : ",
                "Attackable Target Grid Index : ",
                "Moveable Grid Index : ",
                "Attackable Grid Index : "
            };

            // グリッドの状態をメッシュで描画
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                var info = _stageDataProvider.CurrentData.GetTileInfo( i );

                bool[] conditions =
                {
                    Methods.CheckBitFlag(info.flag, TileBitFlag.ATTACKABLE_TARGET_EXIST),
                    Methods.CheckBitFlag(info.flag, TileBitFlag.ATTACKABLE),
                    (0 <= info.estimatedMoveRange),
                    Methods.CheckBitFlag(info.flag, TileBitFlag.REACHABLE_ATTACK)
                };

                for( int j = 0; j < meshTypes.Length; ++j )
                {
                    if( conditions[j] )
                    {
                        var gridMesh = _hierarchyBld.CreateComponentAndOrganize<GridMesh>( _gridMeshObject, true );
                        NullCheck.AssertNotNull( gridMesh, nameof( gridMesh ) );
                        if( gridMesh == null ) continue;

                        _gridMeshs.Add( gridMesh );
                        _gridMeshs[count++].DrawGridMesh( info.charaStandPos, TILE_SIZE, meshTypes[j] );
                        Debug.Log( dbgStrs[j] + i );

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 攻撃可能グリッドを描画します
        /// </summary>
        /// <param name="departIndex">攻撃キャラクターが存在するグリッドのインデックス値</param>
        public void DrawAttackableGrids( int departIndex )
        {
            Debug.Assert( 0 <= departIndex && departIndex < _stageDataProvider.CurrentData.GetTileTotalNum(), "StageController : Irregular Index." );

            int count = 0;
            // グリッドの状態をメッシュで描画
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                if( Methods.CheckBitFlag( _stageDataProvider.CurrentData.GetTileInfo( i ).flag, TileBitFlag.REACHABLE_ATTACK ) )
                {
                    var gridMesh = _hierarchyBld.CreateComponentAndOrganize<GridMesh>( _gridMeshObject, true );
                    NullCheck.AssertNotNull( gridMesh, nameof( gridMesh ) );
                    if( gridMesh == null ) continue;

                    // 攻撃可能なターゲットが存在している場合は、そちらのフラグを優先する
                    MeshType meshType = MeshType.REACHABLE_ATTACK;
                    if( Methods.CheckBitFlag( _stageDataProvider.CurrentData.GetTileInfo( i ).flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                    {
                        meshType = MeshType.ATTACKABLE_TARGET_EXIST;
                    }

                    _gridMeshs.Add( gridMesh );
                    _gridMeshs[count++].DrawGridMesh( _stageDataProvider.CurrentData.GetTileInfo( i ).charaStandPos, TILE_SIZE, meshType );

                    Debug.Log( "Attackable Grid Index : " + i );
                }
            }
        }

        /// <summary>
        /// 全てのグリッドにおける指定のビットフラグの設定を解除します
        /// </summary>
        public void UnsetGridsTileBitFlag( TileBitFlag value )
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
        public void ClearAttackableInfo()
        {
            UnsetGridsTileBitFlag( TileBitFlag.REACHABLE_ATTACK );
            _attackableGridIndexs.Clear();
        }

        /// <summary>
        /// グリッドメッシュにこのクラスを登録します
        /// グリッドメッシュクラスが生成されたタイミングでグリッドメッシュ側から呼び出されます
        /// </summary>
        /// <param name="script">グリッドメッシュクラスのスクリプト</param>
        public void AddGridMeshToList( GridMesh script )
        {
            _gridMeshs.Add( script );
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
                index = _attackableGridIndexs[_gridCursorCtrl.GetAtkTargetIndex()];
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
        /// 攻撃可能グリッドのうち、攻撃可能キャラクターが存在するグリッドをリストに登録します
        /// </summary>
        /// <param name="targetTag">攻撃対象のタグ</param>
        /// <param name="target">予め攻撃対象が決まっている際に指定</param>
        /// <returns>攻撃可能キャラクターが存在している</returns>
        public bool RegistAttackTargetGridIndexs( CHARACTER_TAG targetTag, Character target = null )
        {
            Character character = null;

            _gridCursorCtrl.ClearAtkTargetInfo();
            _attackableGridIndexs.Clear();

            // 攻撃可能、かつ攻撃対象となるキャラクターが存在するグリッドをリストに登録
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                var info = _stageDataProvider.CurrentData.GetTileInfo( i );
                if( Methods.CheckBitFlag( info.flag, TileBitFlag.REACHABLE_ATTACK ) )
                {
                    character = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable( info.charaTag, info.charaIndex );

                    if( character != null && character.Params.CharacterParam.characterTag == targetTag )
                    {
                        _attackableGridIndexs.Add( i );
                    }
                }
            }

            // 選択グリッドを自動的に攻撃可能キャラクターの存在するグリッドインデックスに設定
            if( 0 < _attackableGridIndexs.Count )
            {
                _gridCursorCtrl.SetAtkTargetNum( _attackableGridIndexs.Count );

                // 攻撃対象が既に決まっている場合は対象を探す
                if( target != null && 1 < _attackableGridIndexs.Count )
                {
                    for( int i = 0; i < _attackableGridIndexs.Count; ++i )
                    {
                        var info = GetTileInfo( _attackableGridIndexs[i] );
                        if( target == _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable( info.charaTag, info.charaIndex ) )
                        {
                            _gridCursorCtrl.SetAtkTargetIndex( i );
                            break;
                        }
                    }
                }
                else
                {
                    _gridCursorCtrl.SetAtkTargetIndex( 0 );
                }
            }

            return 0 < _attackableGridIndexs.Count;
        }

        /// <summary>
        /// 指定方向にグリッドを移動させます
        /// </summary>
        /// <param name="direction">グリッドの移動方向</param>
        /// /// <returns>グリッド移動の有無</returns>
        public bool OperateGridCursorController( Constants.Direction direction )
        {
            if( direction == Constants.Direction.FORWARD ) { _gridCursorCtrl.Up(); return true; }
            if( direction == Constants.Direction.BACK ) { _gridCursorCtrl.Down(); return true; }
            if( direction == Constants.Direction.LEFT ) { _gridCursorCtrl.Left(); return true; }
            if( direction == Constants.Direction.RIGHT ) { _gridCursorCtrl.Right(); return true; }

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
            bool updown = ( Math.Abs( fstIndex - scdIndex ) == _stageDataProvider.CurrentData.GridColumnNum );

            int fstQuotient     = fstIndex / _stageDataProvider.CurrentData.GridColumnNum;
            int scdQuotient     = scdIndex / _stageDataProvider.CurrentData.GridColumnNum;
            var fstRemainder    = fstIndex % _stageDataProvider.CurrentData.GridColumnNum;
            var scdRemainder    = scdIndex % _stageDataProvider.CurrentData.GridColumnNum;
            bool leftright      = ( fstQuotient == scdQuotient ) && ( Math.Abs( fstRemainder - scdRemainder ) == 1 );

            return updown || leftright;
        }

        /// <summary>
        /// グリッドに攻撃可能情報を登録します
        /// </summary>
        /// <param name="departIndex">攻撃キャラクターが存在するグリッドのインデックス値</param>
        /// <param name="attackRange">攻撃可能範囲値</param>
        /// <param name="selfTag">攻撃を行うキャラクター自身のキャラクタータグ</param>
        public bool RegistAttackAbleInfo( int departIndex, int attackRange, CHARACTER_TAG selfTag )
        {
            Debug.Assert( 0 <= departIndex && departIndex < _stageDataProvider.CurrentData.GetTileTotalNum(), "StageController : Irregular Index." );

            _attackableGridIndexs.Clear();
            Character attackCandidate = null;

            // 全てのグリッドの攻撃可否情報を初期化
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                Methods.UnsetBitFlag( ref _stageDataProvider.CurrentData.GetTileInfo( i ).flag, TileBitFlag.REACHABLE_ATTACK );
                Methods.UnsetBitFlag( ref _stageDataProvider.CurrentData.GetTileInfo( i ).flag, TileBitFlag.ATTACKABLE );
                Methods.UnsetBitFlag( ref _stageDataProvider.CurrentData.GetTileInfo( i ).flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
            }

            // 攻撃可否情報を各グリッドに登録
            RegistAttackableEachGrid( departIndex, attackRange, selfTag, departIndex );

            // 攻撃可能、かつ攻撃対象となるキャラクターが存在するグリッドをリストに登録
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                var info = _stageDataProvider.CurrentData.GetTileInfo( i );
                if( Methods.CheckBitFlag( info.flag, TileBitFlag.REACHABLE_ATTACK ) )
                {
                    attackCandidate = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable( info.charaTag, info.charaIndex );

                    if( attackCandidate != null && attackCandidate.Params.CharacterParam.characterTag != selfTag )
                    {
                        _attackableGridIndexs.Add( i );
                    }
                }
            }

            return 0 < _attackableGridIndexs.Count;
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
        public List<WaypointInformation> ExtractShortestPath( int departGridIndex, int destGridIndex, int jumpForce, in List<int> candidateRouteIndexs )
        {
            if( departGridIndex == destGridIndex ) { return null; }

            jumpForce = 1;
            Dijkstra dijkstra   = new Dijkstra( candidateRouteIndexs.Count );
            StageData stageData = _stageDataProvider.CurrentData;
            int colNum          = stageData.GridColumnNum;

            // 隣接しているか否かを判定するラムダ式
            Func<int, int, bool> isAdjacent = ( int a, int b ) =>
            {
                int diff = b - a;
                return
                    // 左に存在(左端を除く)
                    ( diff == -1 && ( a % colNum != 0 ) ) ||
                    // 右に存在(右端を除く)
                    ( diff == 1 && ( a % colNum != colNum - 1 ) ) ||
                    // 上または下に存在
                    Math.Abs( diff ) == colNum;
            };

            // ジャンプ可能か否かを判定するラムダ式( 2つのタイルそれぞれの判定を一つの変数に纏めたいので、( bool, bool )の値に格納しています )
            Func<int, int, ( bool, bool )> canJumpOver = ( int a, int b ) =>
            {
                float diffHeight = stageData.GetTileData( b ).Height - stageData.GetTileData( a ).Height;
                return ( ( 0 < diffHeight ) ? (int)Math.Ceiling( diffHeight ) <= jumpForce : true, ( 0 < -diffHeight ) ? (int)Math.Ceiling( -diffHeight ) <= jumpForce : true );
            };

            // 出発グリッドからのインデックスの差を取得
            for( int i = 0; i + 1 < candidateRouteIndexs.Count; ++i )
            {
                for( int j = i + 1; j < candidateRouteIndexs.Count; ++j )
                {
                    if( !isAdjacent( candidateRouteIndexs[i], candidateRouteIndexs[j] ) ) { continue; } // 隣接していなければ次へ

                    (bool, bool) canJump = canJumpOver( candidateRouteIndexs[i], candidateRouteIndexs[j] ); // ジャンプ可能か否かを判定

                    // 移動可能な隣接グリッド情報をダイクストラに入れる
                    if( canJump.Item1 && canJump.Item2 )
                    {
                        dijkstra.Add( i, j );
                        dijkstra.Add( j, i );
                    }
                    else if( canJump.Item1 )
                    {
                        dijkstra.Add( i, j );
                    }
                    else if( canJump.Item2 )
                    {
                        dijkstra.Add( j, i );
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
        /// 移動可能なグリッドを登録します
        /// </summary>
        /// <param name="gridIndex">登録対象のグリッドインデックス</param>
        /// <param name="moveRange">移動可能範囲値</param>
        /// <param name="jumpForce">ジャンプ値</param>
        /// <param name="attackRange">攻撃可能範囲値</param>
        /// <param name="selfTag">呼び出し元キャラクターのキャラクタータグ</param>
        /// <param name="isAttackable">呼び出し元のキャラクターが攻撃可能か否か</param>
        /// <param name="isDeparture">出発グリッドから呼び出されたか否か</param>
        private void RegistMoveableEachGrid( int gridIndex, int moveRange, int attackRange, int jumpForce, int selfCharaIndex, float prevHeight, CHARACTER_TAG selfTag, bool isAttackable, bool isDeparture = false )
        {
            // 範囲外のグリッドは考慮しない
            if( gridIndex < 0 || _stageDataProvider.CurrentData.GetTileTotalNum() <= gridIndex ) return;
            // 指定のタイル情報を取得
            var tileInfo = _stageDataProvider.CurrentData.GetTileInfo( gridIndex );
            if( tileInfo == null ) return;
            // 移動不可のグリッドに辿り着いた場合は終了
            if( Methods.CheckBitFlag( tileInfo.flag, TileBitFlag.CANNOT_MOVE ) ) return;
            // 既に計算済みのグリッドであれば終了
            if( moveRange <= tileInfo.estimatedMoveRange ) return;
            // 自身に対する敵対勢力キャラクターが存在すれば終了
            TileBitFlag[] opponentTag = new TileBitFlag[( int ) CHARACTER_TAG.NUM]
            {
                TileBitFlag.ENEMY_EXIST  | TileBitFlag.OTHER_EXIST,   // PLAYERにおける敵対勢力
                TileBitFlag.ALLY_EXIST | TileBitFlag.OTHER_EXIST,     // ENEMYにおける敵対勢力
                TileBitFlag.ALLY_EXIST | TileBitFlag.ENEMY_EXIST      // OTHERにおける敵対勢力
            };
            if( Methods.CheckBitFlag( tileInfo.flag, opponentTag[( int ) selfTag] ) ) { return; }

            // 直前のタイルとの高さの差分を求め、ジャンプ値と比較して移動可能かを判定する
            float currHeight    = _stageDataProvider.CurrentData.TileDatas[gridIndex].Height;
            float diffHeight    = currHeight - prevHeight;
            int heightRegist    = (int)Math.Ceiling( diffHeight );
            int jumpResist      = ( jumpForce < heightRegist ) ? (jumpForce - heightRegist) : 0;    // ジャンプ値を超過した分を移動抵抗値として加算する(超過していなければ0)

            // 現在グリッドの移動抵抗値を更新( 出発グリッドではmoveRangeの値をそのまま適応する )
            int currentMoveRange = ( isDeparture ) ? moveRange : tileInfo.moveResist + jumpResist + moveRange;
            tileInfo.estimatedMoveRange = currentMoveRange;

            // 負の値であれば終了
            if( currentMoveRange < 0 ) { return; }
            // 攻撃範囲についても登録する
            if( isAttackable && ( tileInfo.charaTag == CHARACTER_TAG.NONE || tileInfo.charaIndex == selfCharaIndex ) )
            {
                RegistAttackableEachGrid( gridIndex, attackRange, selfTag, gridIndex );
            }

            int columnNum = _stageDataProvider.CurrentData.GridColumnNum;

            // 左端を除外
            if( gridIndex % columnNum != 0 )
            {
                RegistMoveableEachGrid( gridIndex - 1, currentMoveRange, attackRange, jumpForce, selfCharaIndex, currHeight, selfTag, isAttackable );      // gridIndexからX軸方向へ-1
            }
            // 右端を除外
            if( ( gridIndex + 1 ) % columnNum != 0 )
            {
                RegistMoveableEachGrid( gridIndex + 1, currentMoveRange, attackRange, jumpForce, selfCharaIndex, currHeight, selfTag, isAttackable );      // gridIndexからX軸方向へ+1
            }
            // Z軸方向への加算と減算はそのまま
            RegistMoveableEachGrid( gridIndex - columnNum, currentMoveRange, attackRange, jumpForce, selfCharaIndex, currHeight, selfTag, isAttackable );  // gridIndexからZ軸方向へ-1
            RegistMoveableEachGrid( gridIndex + columnNum, currentMoveRange, attackRange, jumpForce, selfCharaIndex, currHeight, selfTag, isAttackable );  // gridIndexからZ軸方向へ+1
        }

        /// <summary>
        /// 攻撃可能なグリッドを登録します
        /// </summary>
        /// <param name="gridIndex">対象のグリッドインデックス</param>
        /// <param name="attackRange">攻撃可能範囲値</param>
        /// <param name="selfTag">自身のキャラクタータグ</param>
        /// <param name="departIndex">出発グリッドインデックス</param>
        private void RegistAttackableEachGrid( int gridIndex, int attackRange, CHARACTER_TAG selfTag, int departIndex )
        {
            // 範囲外のグリッドは考慮しない
            if( gridIndex < 0 || _stageDataProvider.CurrentData.GetTileTotalNum() <= gridIndex ) return;
            // 移動不可のグリッドには攻撃できない
            if( Methods.CheckBitFlag( _stageDataProvider.CurrentData.GetTileInfo( gridIndex ).flag, TileBitFlag.CANNOT_MOVE ) ) return;
            // 出発地点でなければ登録
            if( gridIndex != departIndex )
            {
                Methods.SetBitFlag( ref _stageDataProvider.CurrentData.GetTileInfo( gridIndex ).flag, TileBitFlag.REACHABLE_ATTACK );
                var tileInfo = _stageDataProvider.CurrentData.GetTileInfo( gridIndex );

                bool[] isMatch =
                {
                    (tileInfo.charaTag == CHARACTER_TAG.ENEMY || tileInfo.charaTag == CHARACTER_TAG.OTHER),     // PLAYER
                    (tileInfo.charaTag == CHARACTER_TAG.PLAYER || tileInfo.charaTag == CHARACTER_TAG.OTHER),    // ENEMY
                    (tileInfo.charaTag == CHARACTER_TAG.PLAYER || tileInfo.charaTag == CHARACTER_TAG.ENEMY)     // OTHER
                };

                if( isMatch[( int ) selfTag] )
                {
                    Methods.SetBitFlag( ref _stageDataProvider.CurrentData.GetTileInfo( departIndex ).flag, TileBitFlag.ATTACKABLE );
                    Methods.SetBitFlag( ref _stageDataProvider.CurrentData.GetTileInfo( gridIndex ).flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
                }
            }

            // 負の値であれば終了
            if( --attackRange < 0 ) return;

            // 左端を除外
            if( gridIndex % _stageDataProvider.CurrentData.GridColumnNum != 0 )
                RegistAttackableEachGrid( gridIndex - 1, attackRange, selfTag, departIndex );       // gridIndexからX軸方向へ-1
                                                                                                    // 右端を除外
            if( ( gridIndex + 1 ) % _stageDataProvider.CurrentData.GridColumnNum != 0 )
                RegistAttackableEachGrid( gridIndex + 1, attackRange, selfTag, departIndex );       // gridIndexからX軸方向へ+1
                                                                                                    // Z軸方向への加算と減算はそのまま
            RegistAttackableEachGrid( gridIndex - _stageDataProvider.CurrentData.GridColumnNum, attackRange, selfTag, departIndex );   // gridIndexからZ軸方向へ-1
            RegistAttackableEachGrid( gridIndex + _stageDataProvider.CurrentData.GridColumnNum, attackRange, selfTag, departIndex );   // gridindexからZ軸方向へ+1
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