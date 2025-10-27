using Froniter.Entities;
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
        private ActionableTileMap _actionableTileMap            = null;
        private List<TileMesh> _attackableTileMeshes            = new List<TileMesh>();

        public ActionableTileMap ActionableTileMap { get { return _actionableTileMap; } }
        public void Init( Character owner )
        {
            _owner                  = owner;

            if( null == _actionableTileMap )
            {
                _actionableTileMap = _hierarchyBld.InstantiateWithDiContainer<ActionableTileMap>( false );
                NullCheck.AssertNotNull( _actionableTileMap, "_actionableTileMap" );
            }
            _actionableTileMap.Init();
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
                DrawTileMashes( _actionableTileMap, in color );
            }
        }

        public void SetActionableTileDatas( ActionableTileMap actionableTileMap )
        {
            _actionableTileMap = actionableTileMap;
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