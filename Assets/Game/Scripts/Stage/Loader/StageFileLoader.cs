using Frontier.DebugTools.StageEditor;
using Frontier.Entities;
using Frontier.Registries;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Stage
{
    public sealed class StageFileLoader : MonoBehaviour
    {
        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private PrefabRegistry _prefabReg              = null;
        [Inject] private FilePathRegistry _filePathReg          = null;

        private GameObject[] _tilePrefabs;

        /// <summary>直近のロードで生成した StageProp 一覧。StageController が占有タイル登録に使用します。</summary>
        public IReadOnlyList<StagePropDataSerializer.StagePropStatusData> LastLoadedStagePropData => _lastLoadedStagePropData;
        private List<StagePropDataSerializer.StagePropStatusData> _lastLoadedStagePropData = null;

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="tilePregabs">ステージのタイルを配置する際に参照するタイルデータのプレハブ群</param>
        public void Init( GameObject[] tilePregabs )
        {
            NullCheck.AssertNotNull( _stageDataProvider , nameof( _stageDataProvider ) );
            NullCheck.AssertNotNull( _hierarchyBld , nameof( _hierarchyBld ) );

            _tilePrefabs = tilePregabs;
        }

        /// <summary>
        /// ステージデータをファイル名を指定することで読み込みます
        /// </summary>
        /// <param name="fileName">指定するファイル名</param>
        /// <returns>読込の成否</returns>
        public bool Load( string fileName )
        {
            var loadData = StageDataSerializer.Load( fileName );
            if( loadData == null ) { return false; }

            // 既存のステージデータが存在する場合は破棄
            if( null != _stageDataProvider.CurrentData )
            {
                _stageDataProvider.CurrentData.Dispose();
            }
            // 存在しない場合は作成
            else
            {
                _stageDataProvider.CurrentData = _hierarchyBld.InstantiateWithDiContainer<StageData>( false );
            }

            var deployableNum   = loadData.MaxDeployableUnits;
            var row             = loadData.TileRowNum;
            var col             = loadData.TileColNum;
            _stageDataProvider.CurrentData.Init( deployableNum, row, col ); // 新しいステージデータを初期化

            for( int x = 0; x < col; x++ )
            {
                for( int y = 0; y < row; y++ )
                {
                    var stgData     = _stageDataProvider.CurrentData;
                    var saveData    = loadData.GetSaveData( x, y );
                    stgData.SetTile( x, y, _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Tile>( _tilePrefabs[0], true, false, $"Tile_X{x}_Y{y}" ) );
                    stgData.GetTile( x, y ).Init( x, y, saveData.IsDeployable, saveData.Height, saveData.TileType );
                }
            }

            // StageProp を配置し、占有タイルを移動不可にする
            _lastLoadedStagePropData = StagePropDataSerializer.Load( fileName );
            if ( _lastLoadedStagePropData != null )
            {
                foreach ( var prop in _lastLoadedStagePropData )
                {
                    _stageDataProvider.CurrentData.GetTileStaticData( prop.TileIndex ).MoveResist = short.MaxValue;
                    SpawnStageProp( prop );
                }
            }

            return true;
        }

        /// <summary>
        /// ステージデータをファイル名配列にインデックスを指定する形で読込みます
        /// </summary>
        /// <param name="stageNameIdx">ステージ名配列へのインデックス値</param>
        /// <returns>読込の成否</returns>
        public bool Load( int stageNameIdx )
        {
            return Load( _filePathReg.StageNames[stageNameIdx] );
        }

        private void SpawnStageProp( StagePropDataSerializer.StagePropStatusData data )
        {
            if ( _prefabReg?.StagePropPrefabs == null ) return;
            if ( data.Prefab < 0 || _prefabReg.StagePropPrefabs.Length <= data.Prefab ) return;

            var prop = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StageProp>(
                _prefabReg.StagePropPrefabs[data.Prefab], true, true, $"[StageProp_{data.TileIndex}]" );
            if ( prop == null ) return;

            prop.SetSize( data.Size );
            var pos = GridPositionUtility.CalcSizeAwareCenter( data.TileIndex, data.Size, _stageDataProvider );
            prop.SetPosition( pos );
            prop.SetRotation( ( Direction ) data.Direction );
        }
    }
}