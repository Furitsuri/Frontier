using Frontier.Combat.Skill;
using Frontier.DebugTools.StageEditor;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static UnityEngine.Rendering.DebugUI.Table;

namespace Frontier.Stage
{
    public class StageFileLoader : MonoBehaviour
    {
        [SerializeField]
        private List<string> _stageNames;

        [Header("Prefabs")]
        [SerializeField]
        public GameObject[] tilePrefabs;

        private HierarchyBuilderBase _hierarchyBld;

        private void Construct( HierarchyBuilderBase hierarchyBld )
        {
            _hierarchyBld = hierarchyBld;
        }

        public bool Load( int stageIndex, ref StageData stageData )
        {
            var data = StageDataSerializer.Load(_stageNames[stageIndex]);
            if ( data == null ) return false;

            var row     = data.GridRowNum;
            var column  = data.GridColumnNum;
            stageData.Init( row, column ); // 新しいステージデータを初期化

            for ( int y = 0; y < column; y++ )
            {
                for ( int x = 0; x < row; x++ )
                {
                    var srcTile = data.GetTile(x, y);
                    stageData.SetTile( x, y, _hierarchyBld.InstantiateWithDiContainer<StageTileData>( false ) );
                    var applyTile = stageData.GetTile( x, y );
                    applyTile.SetTileTypeAndHeight( ( TileType )srcTile.Type, srcTile.Height );
                    applyTile.InstantiateTileInfo( x + y * stageData.GridRowNum, stageData.GridRowNum, _hierarchyBld );
                    applyTile.InstantiateTileBhv( x, y, tilePrefabs, _hierarchyBld );
                    applyTile.InstantiateTileMesh( _hierarchyBld );
                }
            }

            return true;
        }
    }
}