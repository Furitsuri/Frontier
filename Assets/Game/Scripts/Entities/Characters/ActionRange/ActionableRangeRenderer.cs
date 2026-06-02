using Frontier.Registries;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using Zenject;

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

        private bool _isShowingAttackableRange                          = false;
        private bool _isDisplayingQueuedRange                           = false;
        private Character _owner                                        = null;
        private ReadOnlyReference<ActionableTileData> _readOnlyActionableTileData;

        /// <summary>
        /// 予約済み表示のタイルインデックス（オーナータイル + TargetableTileMap タイル）
        /// </summary>
        private readonly List<int> _queuedDisplayTileIndices = new List<int>();

        public bool IsShowingAttackableRange  => _isShowingAttackableRange;
        public bool IsDisplayingQueuedRange   => _isDisplayingQueuedRange;

        public void Init( Character owner, ActionableTileData actionableTileMap )
        {
            _isShowingAttackableRange   = false;
            _isDisplayingQueuedRange    = false;
            _owner                      = owner;
            _readOnlyActionableTileData = new ReadOnlyReference<ActionableTileData>( actionableTileMap );
            _queuedDisplayTileIndices.Clear();
        }

        public void Dispose()
        {
            _owner = null;
            _readOnlyActionableTileData = null;
            _queuedDisplayTileIndices.Clear();
        }

        /// <summary>
        /// 攻撃範囲の表示・非表示を切り替えます
        /// </summary>
        /// <param name="isShow"></param>
        /// <param name="color"></param>
        public void SetDisplayDangerRange( bool isShow, in UnityEngine.Color color )
        {
            if( isShow == _isShowingAttackableRange ) { return; }

            _isShowingAttackableRange = isShow;

            if( _isShowingAttackableRange )
            {
                DrawDangerRange( in color );
            }
            else
            {
                ClearTileMeshes();
            }
        }

        /// <summary>
        /// 移動可能領域を描画します
        /// </summary>
        /// <param name="conditionBuilder"></param>
        public void DrawMoveableRange( Func<TileDynamicData, (MeshType meshType, bool condition)[]> conditionBuilder )
        {
            foreach( var data in _readOnlyActionableTileData.Value.MoveableTileMap )
            {
                var meshTypeAndConditions = conditionBuilder( data.Value );

                for( int i = 0; i < meshTypeAndConditions.Length; ++i )
                {
                    if( meshTypeAndConditions[i].condition )
                    {
                        TileMesh tileMesh = null;
                        LazyInject.GetOrCreate( ref tileMesh, () => _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true ) );

                        var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                        tile.DrawTileMesh( tileMesh, in TileColors.Colors[( int ) meshTypeAndConditions[i].meshType], _owner.GetCharacterKey() );

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 攻撃可能領域を描画します
        /// </summary>
        /// <param name="conditionBuilder"></param>
        public void DrawAttackableRange( Func<TileDynamicData, (MeshType meshType, bool condition)[]> conditionBuilder )
        {
            // 攻撃可能なタイルの描画
            foreach( var data in _readOnlyActionableTileData.Value.AttackableTileMap )
            {
                var meshTypeAndConditions = conditionBuilder( data.Value );

                for( int i = 0; i < meshTypeAndConditions.Length; ++i )
                {
                    if( meshTypeAndConditions[i].condition )
                    {
                        TileMesh tileMesh = null;
                        LazyInject.GetOrCreate( ref tileMesh, () => _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true ) );

                        var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                        tile.DrawTileMesh( tileMesh, in TileColors.Colors[( int ) meshTypeAndConditions[i].meshType], _owner.GetCharacterKey() );

                        break;
                    }
                }
            }

            // ターゲット可能なタイルがあれば、上記のタイルメッシュよりも上に描画
            foreach( var data in _readOnlyActionableTileData.Value.TargetableTileMap )
            {
                TileMesh tileMesh = null;
                LazyInject.GetOrCreate( ref tileMesh, () => _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true ) );

                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                tile.DrawTileMesh( tileMesh, in TileColors.Colors[( int ) MeshType.TARGETABLE], _owner.GetCharacterKey() );
            }
        }

        /// <summary>
        /// ターゲット可能範囲の既存描画を消去した後、オーナーのタイルと TargetableTileMap を
        /// TARGETABLE_QUEUE 色で描画し、各タイルインデックスを _queuedDisplayTileIndices に記録します。
        /// </summary>
        public void DrawTargetableRangeAsQueued()
        {
            // TargetableTileMap の既存描画を消去
            foreach( var data in _readOnlyActionableTileData.Value.TargetableTileMap )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                tile?.ClearTileMesh( _owner.GetCharacterKey() );
            }
            _queuedDisplayTileIndices.Clear();

            // オーナーの現在タイルを描画・記録
            DrawQueuedTile( _owner.BattleParams.TmpParam.CurrentTileIndex );

            // TargetableTileMap のタイルを描画・記録
            foreach( var data in _readOnlyActionableTileData.Value.TargetableTileMap )
            {
                DrawQueuedTile( data.Key );
            }

            _isDisplayingQueuedRange = true;
        }

        /// <summary>
        /// DrawTargetableRangeAsQueued() で描画した予約済み表示を消去します。
        /// キューに積まれたスキルの実行時に呼んでください。
        /// </summary>
        public void ClearQueuedRangeDisplay()
        {
            ClearQueuedDisplayInternal();
        }

        /// <summary>
        /// 予約済み攻撃範囲のタイルメッシュを点滅させます。
        /// DrawTargetableRangeAsQueued() で描画済みの状態で呼ぶことを想定しています。
        /// </summary>
        public void SetBlinkTargetableRange( bool isBlink )
        {
            foreach( var tileIndex in _queuedDisplayTileIndices )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( tileIndex );
                if( tile == null ) { continue; }
                tile.GetTileMeshByOwnerKey( _owner.GetCharacterKey() )?.SetBlink( isBlink );
            }
        }

        /// <summary>
        /// 各タイルに登録したタイルメッシュの描画を全て消去します
        /// </summary>
        public void ClearTileMeshes()
        {
            foreach( var data in _readOnlyActionableTileData.Value.TargetableTileMap )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                NullCheck.AssertNotNull( tile, nameof( tile ) );
                tile.ClearTileMesh( _owner.GetCharacterKey() );
            }
            foreach( var data in _readOnlyActionableTileData.Value.AttackableTileMap )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                NullCheck.AssertNotNull( tile, nameof( tile ) );
                tile.ClearTileMesh( _owner.GetCharacterKey() );
            }
            foreach( var data in _readOnlyActionableTileData.Value.MoveableTileMap )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                NullCheck.AssertNotNull( tile, nameof( tile ) );
                tile.ClearTileMesh( _owner.GetCharacterKey() );
            }

            // ActionableTileData に含まれないオーナータイルなどの予約済み表示も消去
            ClearQueuedDisplayInternal();

            _isShowingAttackableRange = false;
        }

        private void DrawQueuedTile( int tileIndex )
        {
            TileMesh tileMesh = null;
            LazyInject.GetOrCreate( ref tileMesh, () => _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true ) );
            var tile = _stageDataProvider.CurrentData.GetTile( tileIndex );
            if( tile == null ) { return; }
            tile.DrawTileMesh( tileMesh, in TileColors.Colors[( int ) MeshType.TARGETABLE_QUEUE], _owner.GetCharacterKey() );
            _queuedDisplayTileIndices.Add( tileIndex );
        }

        private void ClearQueuedDisplayInternal()
        {
            foreach( var tileIndex in _queuedDisplayTileIndices )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( tileIndex );
                tile?.ClearTileMesh( _owner.GetCharacterKey() );
            }
            _queuedDisplayTileIndices.Clear();
            _isDisplayingQueuedRange = false;
        }

        /// <summary>
        /// 危険領域を描画します
        /// </summary>
        /// <param name="color"></param>
        private void DrawDangerRange( in UnityEngine.Color color )
        {
            foreach( var data in _readOnlyActionableTileData.Value.AttackableTileMap )
            {
                TileMesh tileMesh = null;
                LazyInject.GetOrCreate( ref tileMesh, () => _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true ) );

                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                tile.DrawTileMesh( tileMesh, in color, _owner.GetCharacterKey() );
            }
        }
    }
}