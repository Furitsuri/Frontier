using Frontier.Combat.Skill;
using Frontier.DebugTools.StageEditor;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Stage
{
    public sealed class StageFileLoader : MonoBehaviour
    {
        [SerializeField] private List<string> _stageNames;

        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;

        private GameObject[] _tileBhvPrefabs;
        private GameObject[] _tilePrefabs;

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="tilePregabs">ステージのタイルを配置する際に参照するタイルデータのプレハブ群</param>
        public void Init( GameObject[] tilePregabs, GameObject[] tileBhvPrefabs )
        {
            NullCheck.AssertNotNull( _stageDataProvider , nameof( _stageDataProvider ) );
            NullCheck.AssertNotNull( _hierarchyBld , nameof( _hierarchyBld ) );

            _tileBhvPrefabs = tileBhvPrefabs;
            _tilePrefabs = tilePregabs;
        }

        /// <summary>
        /// ステージデータをファイル名を指定することで読み込みます
        /// </summary>
        /// <param name="fileName">指定するファイル名</param>
        /// <returns>読込の成否</returns>
        public bool Load( string fileName )
        {
            var loadData = StageDataSerializer.Load(fileName);
            if ( loadData == null ) { return false; }

            // 既存のステージデータが存在する場合は破棄
            if ( null != _stageDataProvider.CurrentData )
            {
                _stageDataProvider.CurrentData.Dispose();
            }
            // 存在しない場合は作成
            else
            {
                _stageDataProvider.CurrentData = _hierarchyBld.InstantiateWithDiContainer<StageData>( false );
            }

            var row = loadData.TileRowNum;
            var col = loadData.TileColNum;
            _stageDataProvider.CurrentData.Init( row, col ); // 新しいステージデータを初期化

            for ( int x = 0; x < col; x++ )
            {
                for ( int y = 0; y < row; y++ )
                {
                    var stgData = _stageDataProvider.CurrentData;
                    var loadStaticData = loadData.GetStaticData( x, y );
                    stgData.SetStaticData( x, y, _hierarchyBld.InstantiateWithDiContainer<TileStaticData>( false ) );
                    stgData.GetStaticData( x, y ).Init( x, y, loadStaticData.Height, loadStaticData.TileType );
                    stgData.SetTile( x, y, _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Tile>( _tilePrefabs[0], true, false, $"Tile_X{x}_Y{y}" ) );
                    stgData.GetTile( x, y ).Init( x, y, loadStaticData.Height, loadStaticData.TileType );
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
            return Load( _stageNames[stageNameIdx] );
        }
    }
}