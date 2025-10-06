using Froniter.Registries;
using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Entities
{
    public class AttackableRangeHandler
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private StageController _stageCtrl         = null;
        [Inject] protected PrefabRegistry _prefabReg        = null;

        protected bool _isDisplayAttackableRange = false;
        protected List<AttackableRangeData> _attackableRanges = new List<AttackableRangeData>();
        protected List<GridMesh> _attackableTileMeshes = new List<GridMesh>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cParams"></param>
        /// <param name="tileCostTable"></param>
        /// <param name="color"></param>
        public void ToggleAttackableRangeDisplay( in CharacterParameters cParams, in int[] tileCostTable, Color color )
        {
            _isDisplayAttackableRange = !_isDisplayAttackableRange;

            if( !_isDisplayAttackableRange )
            {
                UnsetAttackableRangeDisplay();
            }
            else
            {
                List<int> attackableTileIndexs = new List<int>();

                var param           = cParams.CharacterParam;
                int tileIndex       = cParams.TmpParam.gridIndex;
                float tileHeight    = _stageCtrl.GetTileData( tileIndex ).Height;
                _stageCtrl.TileInfoDataHdlr().UpdateTileInfo();
                _stageCtrl.TileInfoDataHdlr().BeginRegisterMoveableTiles( tileIndex, param.moveRange, param.attackRange, param.jumpForce, param.characterIndex, tileHeight, tileCostTable, param.characterTag, true );

                for( int i = 0; i < _stageCtrl.GetTileTotalNum(); ++i )
                {
                    var info = _stageCtrl.GetTileInfo( i );
                    if( Methods.CheckBitFlag( info.flag, TileBitFlag.ATTACKABLE ) )
                    {
                        attackableTileIndexs.Add( i );
                    }
                }

                DrawTileMashes( attackableTileIndexs, TileColors.Colors[( int ) MeshType.ATTACKABLE] );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void UnsetAttackableRangeDisplay()
        {
            _isDisplayAttackableRange = false;

            ClearTileMeshes();
        }

        public void DrawTileMashes( List<int> tileIndexs, Color color )
        {
            for( int i = 0; i < tileIndexs.Count; ++i )
            {
                var info = _stageCtrl.GetTileInfo( tileIndexs.ElementAt( i ) );
                NullCheck.AssertNotNull( info, nameof( info ) );
                var tileMesh = _hierarchyBld.CreateComponentAndOrganize<GridMesh>( _prefabReg.TileMeshPrefab, true );
                NullCheck.AssertNotNull( tileMesh, nameof( tileMesh ) );

                _attackableTileMeshes.Add( tileMesh );
                _attackableTileMeshes[i].DrawTileMesh( info.charaStandPos, TILE_SIZE, color );
            }
        }

        public void ClearTileMeshes()
        {
            foreach( var tile in _attackableTileMeshes )
            {
                tile.ClearDraw();
                tile.Remove();
            }
            _attackableTileMeshes.Clear();
        }
    }
}