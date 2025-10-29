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
    public class ActionableRangeRenderer
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private PrefabRegistry _prefabReg              = null;
        [Inject] private IStageDataProvider _stageDataProvider  = null;

        private bool _isShowingAttackableRange                  = false;
        private Character _owner                                = null;
        private ReadOnlyReference<ActionableTileMap> _readOnlyActionableTileMap;

        public bool IsShowingAttackableRange => _isShowingAttackableRange;

        public void Init( Character owner, ActionableTileMap actionableTileMap )
        {
            _isShowingAttackableRange   = false;
            _owner                      = owner;
            _readOnlyActionableTileMap  = new ReadOnlyReference<ActionableTileMap>( actionableTileMap );
        }

        /// <summary>
        /// 攻撃範囲の表示・非表示を切り替えます
        /// </summary>
        /// <param name="isShow"></param>
        /// <param name="color"></param>
        public void SetAttackableRangeDisplay( bool isShow, in UnityEngine.Color color )
        {
            if( isShow == _isShowingAttackableRange ) { return; }

            _isShowingAttackableRange = isShow;

            if( _isShowingAttackableRange )
            {
                DrawTileMashes( in color );
            }
            else
            {
                ClearTileMeshes();
            }
        }

        /// <summary>
        /// 攻撃可能領域を描画します
        /// </summary>
        /// <param name="color"></param>
        public void DrawTileMashes( in UnityEngine.Color color )
        {
            foreach( var data in _readOnlyActionableTileMap.Value.AttackableTileMap )
            {
                var tileMesh = _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true );
                NullCheck.AssertNotNull( tileMesh, nameof( tileMesh ) );

                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                tile.DrawTileMesh( tileMesh, in color, _owner.CharaKey );
            }
        }

        /// <summary>
        /// アクション可能領域を描画します
        /// </summary>
        /// <param name="actionableTileMap"></param>
        /// <param name="conditionBuilder"></param>
        public void DrawActionableRange( ActionableTileMap actionableTileMap, Func<TileDynamicData, (MeshType meshType, bool condition)[]> conditionBuilder )
        {
            foreach( var data in actionableTileMap.AttackableTileMap )
            {
                var meshTypeAndConditions = conditionBuilder( data.Value );

                for( int j = 0; j < meshTypeAndConditions.Length; ++j )
                {
                    if( meshTypeAndConditions[j].condition )
                    {
                        var tileMesh = _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true );
                        NullCheck.AssertNotNull( tileMesh, nameof( tileMesh ) );

                        var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                        tile.DrawTileMesh( tileMesh, in TileColors.Colors[( int ) meshTypeAndConditions[j].meshType], _owner.CharaKey );

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 各タイルに登録したタイルメッシュを全て削除します
        /// </summary>
        public void ClearTileMeshes()
        {
            foreach( var data in _readOnlyActionableTileMap.Value.AttackableTileMap )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                NullCheck.AssertNotNull( tile, nameof( tile ) );
                tile.ClearTileMesh( _owner.CharaKey );
            }

            _isShowingAttackableRange = false;
        }
    }
}