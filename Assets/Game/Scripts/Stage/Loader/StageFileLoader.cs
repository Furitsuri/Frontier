using Frontier.Combat.Skill;
using Frontier.DebugTools.StageEditor;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Stage
{
    public class StageFileLoader : MonoBehaviour
    {
        [SerializeField]
        private List<string> _stageNames;

        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;

        private GameObject[] _tilePrefabs;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tilePregabs"></param>
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
            var data = StageDataSerializer.Load(fileName);
            if ( data == null ) { return false; }

            // 既存のステージデータが存在する場合は破棄
            if ( null != _stageDataProvider.CurrentData )
            {
                _stageDataProvider.CurrentData.Dispose();
            }

            var row = data.GridRowNum;
            var col = data.GridColumnNum;
            _stageDataProvider.CurrentData.Init( row, col ); // 新しいステージデータを初期化

            for ( int x = 0; x < col; x++ )
            {
                for ( int y = 0; y < row; y++ )
                {
                    var srcTile = data.GetTile(x, y);
                    _stageDataProvider.CurrentData.SetTile( x, y, _hierarchyBld.InstantiateWithDiContainer<StageTileData>( false ) );
                    _stageDataProvider.CurrentData.GetTile( x, y ).Init( x, y, srcTile.Height, srcTile.Type, _tilePrefabs );
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