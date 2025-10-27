using Froniter.Entities;
using Froniter.Registries;
using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// 攻撃可能範囲の取得、及びその表示・非表示を切り替えるハンドラです
    /// </summary>
    public class AttackableRangeHandler
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private StageController _stageCtrl         = null;
        [Inject] protected PrefabRegistry _prefabReg        = null;

        private bool _isDisplayAttackableRange                  = false;
        private Character _owner                                = null;
        private List<TileMesh> _attackableTileMeshes            = new List<TileMesh>();
        private ReadOnlyReference<ActionableTileMap> _readOnlyActionableTileMap;

        public void Init( Character owner, ActionableTileMap actionableTileMap )
        {
            _owner                      = owner;
            _readOnlyActionableTileMap  = new ReadOnlyReference<ActionableTileMap>( actionableTileMap );
        }

        /// <summary>
        /// 攻撃範囲の表示を切り替えます
        /// </summary>
        /// <param name="cParams"></param>
        /// <param name="tileCostTable"></param>
        /// <param name="color"></param>
        public void ToggleAttackableRangeDisplay( in Color color )
        {
            _isDisplayAttackableRange = !_isDisplayAttackableRange;

            if( !_isDisplayAttackableRange )
            {
                UnsetAttackableRangeDisplay();
            }
            else
            {
                DrawTileMashes( _readOnlyActionableTileMap.Value, in color );
            }
        }

        /// <summary>
        /// 攻撃範囲の表示を解除します
        /// </summary>
        public void UnsetAttackableRangeDisplay()
        {
            _isDisplayAttackableRange = false;

            ClearTileMeshes();
        }

        public void DrawTileMashes( ActionableTileMap actionableTileMap, in Color color )
        {
            int count = 0;
            foreach( var data in actionableTileMap.AttackableTileMap )
            {
                var tileSData = _stageCtrl.GetTileStaticData( data.Key );
                NullCheck.AssertNotNull( tileSData, nameof( tileSData ) );
                var tileMesh = _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true );
                NullCheck.AssertNotNull( tileMesh, nameof( tileMesh ) );

                _attackableTileMeshes.Add( tileMesh );
                _attackableTileMeshes[count++].DrawTileMesh( tileSData.CharaStandPos, TILE_SIZE, color );
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