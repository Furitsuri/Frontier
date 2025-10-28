using Froniter.Entities;
using Froniter.Registries;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private PrefabRegistry _prefabReg              = null;
        [Inject] private IStageDataProvider _stageDataProvider  = null;

        private bool _isDisplayAttackableRange                  = false;
        private Character _owner                                = null;
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
        public void ToggleAttackableRangeDisplay( in UnityEngine.Color color )
        {
            _isDisplayAttackableRange = !_isDisplayAttackableRange;

            if( !_isDisplayAttackableRange )
            {
                UnsetAttackableRangeDisplay();
            }
            else
            {
                DrawTileMashes( in color );
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

        public void DrawTileMashes( in UnityEngine.Color color )
        {
            foreach( var data in _readOnlyActionableTileMap.Value.AttackableTileMap )
            {
                var tileMesh = _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true );
                NullCheck.AssertNotNull( tileMesh, nameof( tileMesh ) );

                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                tile.DrawTileMesh( tileMesh, color );
            }
        }

        public void ClearTileMeshes()
        {
            foreach( var data in _readOnlyActionableTileMap.Value.AttackableTileMap )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                NullCheck.AssertNotNull( tile, nameof( tile ) );
                tile.ClearTileMeshes();
            }
        }
    }
}