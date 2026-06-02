using Frontier.Registries;
using Frontier.Stage;
using System;
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

        private bool _isShowingAttackableRange = false;
        private bool _isDisplayingQueuedRange  = false;
        private Character _owner               = null;
        private ReadOnlyReference<ActionableTileData> _readOnlyActionableTileData;

        public bool IsShowingAttackableRange => _isShowingAttackableRange;
        public bool IsDisplayingQueuedRange  => _isDisplayingQueuedRange;

        public void Init( Character owner, ActionableTileData actionableTileMap )
        {
            _isShowingAttackableRange   = false;
            _isDisplayingQueuedRange    = false;
            _owner                      = owner;
            _readOnlyActionableTileData = new ReadOnlyReference<ActionableTileData>( actionableTileMap );
        }

        public void Dispose()
        {
            _owner = null;
            _readOnlyActionableTileData = null;
        }

        /// <summary>
        /// 攻撃範囲の表示・非表示を切り替えます
        /// </summary>
        public void SetDisplayDangerRange( bool isShow, in UnityEngine.Color color )
        {
            if( isShow == _isShowingAttackableRange ) { return; }

            _isShowingAttackableRange = isShow;

            if( _isShowingAttackableRange ) { DrawDangerRange( in color ); }
            else { ClearTileMeshes(); }
        }

        /// <summary>
        /// Characterの持つActionableTileDataのうち、指定されたTileMapTypeに該当する範囲を描画します
        /// </summary>
        public void DrawRange( TileMapType tileMapType, Func<TileDynamicData, (MeshType meshType, bool condition)[]> conditionBuilder )
        {
            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( tileMapType ) )
            {
                var meshTypeAndConditions = conditionBuilder( data.Value );

                for( int i = 0; i < meshTypeAndConditions.Length; ++i )
                {
                    if( meshTypeAndConditions[i].condition )
                    {
                        TileMesh tileMesh = null;
                        LazyInject.GetOrCreate( ref tileMesh, () => _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true ) );

                        var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                        tile.DrawTileMesh( tileMesh, in TileColors.Colors[( int ) meshTypeAndConditions[i].meshType], _owner.GetCharacterKey(), tileMapType );

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// TargetableTileMap の既存描画を消去した後、オーナーのタイルと TargetableTileMap を
        /// QUEUED 色で描画し、タイルインデックスを ActionableTileData の QUEUED マップに記録します。
        /// </summary>
        public void DrawTargetableRangeAsQueued()
        {
            // TargetableTileMap の既存描画を消去
            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( TileMapType.TARGETABLE ) )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                tile?.ClearTileMesh( _owner.GetCharacterKey() );
            }
            _readOnlyActionableTileData.Value.ClearTileMap( TileMapType.QUEUED );

            // オーナーの現在タイルを QUEUED として描画・記録
            DrawQueuedTile( _owner.BattleParams.TmpParam.CurrentTileIndex );

            // TargetableTileMap のタイルを QUEUED として描画・記録
            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( TileMapType.TARGETABLE ) )
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
        /// 指定した TileMapType（複数可）に該当するタイルの描画を消去します。
        /// ビットフラグで複数種を同時に指定できます。
        /// </summary>
        public void ClearTileMeshesByType( TileMapType types )
        {
            CharacterKey ownerKey = _owner.GetCharacterKey();
            int typeValue         = ( int ) types;

            for( int bit = 1; bit <= ( int ) TileMapType.QUEUED; bit <<= 1 )
            {
                if( ( typeValue & bit ) == 0 ) { continue; }

                var singleType = ( TileMapType ) bit;
                foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( singleType ) )
                {
                    var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                    tile?.ClearTileMesh( ownerKey, singleType );
                }

                if( singleType == TileMapType.QUEUED )
                {
                    _readOnlyActionableTileData.Value.ClearTileMap( TileMapType.QUEUED );
                    _isDisplayingQueuedRange = false;
                }
            }

            if( ( types & ( TileMapType.MOVEABLE | TileMapType.ATTACKABLE | TileMapType.TARGETABLE ) ) != 0 )
            {
                _isShowingAttackableRange = false;
            }
        }

        /// <summary>
        /// 予約済み攻撃範囲のタイルメッシュを点滅させます。
        /// DrawTargetableRangeAsQueued() で描画済みの状態で呼ぶことを想定しています。
        /// </summary>
        public void SetBlinkTargetableRange( bool isBlink )
        {
            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( TileMapType.QUEUED ) )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                if( tile == null ) { continue; }
                tile.GetTileMeshByOwnerKey( _owner.GetCharacterKey() )?.SetBlink( isBlink );
            }
        }

        /// <summary>
        /// 各タイルに登録したタイルメッシュの描画を全て消去します
        /// </summary>
        public void ClearTileMeshes()
        {
            CharacterKey ownerKey = _owner.GetCharacterKey();

            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( TileMapType.TARGETABLE ) )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                NullCheck.AssertNotNull( tile, nameof( tile ) );
                tile.ClearTileMesh( ownerKey );
            }
            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( TileMapType.ATTACKABLE ) )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                NullCheck.AssertNotNull( tile, nameof( tile ) );
                tile.ClearTileMesh( ownerKey );
            }
            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( TileMapType.MOVEABLE ) )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                NullCheck.AssertNotNull( tile, nameof( tile ) );
                tile.ClearTileMesh( ownerKey );
            }

            // ActionableTileData 外（オーナータイルなど）の QUEUED 描画も消去
            ClearQueuedDisplayInternal();

            _isShowingAttackableRange = false;
        }

        private void DrawQueuedTile( int tileIndex )
        {
            TileMesh tileMesh = null;
            LazyInject.GetOrCreate( ref tileMesh, () => _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true ) );
            var tile = _stageDataProvider.CurrentData.GetTile( tileIndex );
            if( tile == null ) { return; }
            tile.DrawTileMesh( tileMesh, in TileColors.Colors[( int ) MeshType.QUEUED], _owner.GetCharacterKey(), TileMapType.QUEUED );
            _readOnlyActionableTileData.Value.AddQueuedTile( tileIndex );
        }

        private void ClearQueuedDisplayInternal()
        {
            CharacterKey ownerKey = _owner.GetCharacterKey();
            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( TileMapType.QUEUED ) )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                tile?.ClearTileMesh( ownerKey, TileMapType.QUEUED );
            }
            _readOnlyActionableTileData.Value.ClearTileMap( TileMapType.QUEUED );
            _isDisplayingQueuedRange = false;
        }

        private void DrawDangerRange( in UnityEngine.Color color )
        {
            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( TileMapType.ATTACKABLE ) )
            {
                TileMesh tileMesh = null;
                LazyInject.GetOrCreate( ref tileMesh, () => _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true ) );

                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                tile.DrawTileMesh( tileMesh, in color, _owner.GetCharacterKey(), TileMapType.ATTACKABLE );
            }
        }
    }
}
