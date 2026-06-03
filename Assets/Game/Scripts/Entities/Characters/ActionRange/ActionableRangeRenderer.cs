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
        private Character _owner               = null;
        private ReadOnlyReference<ActionableTileData> _readOnlyActionableTileData;

        public bool IsShowingAttackableRange => _isShowingAttackableRange;

        public void Init( Character owner, ActionableTileData actionableTileMap )
        {
            _isShowingAttackableRange   = false;
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
        /// 指定した TileMapType（複数可）に該当するタイルの描画を消去します。
        /// ビットフラグで複数種を同時に指定できます。
        /// </summary>
        public void ClearTileMeshesByType( TileMapType types )
        {
            CharacterKey ownerKey = _owner.GetCharacterKey();

            foreach( TileMapType singleType in Enum.GetValues( typeof( TileMapType ) ) )
            {
                if( singleType == TileMapType.NONE ) { continue; }
                if( !types.HasFlag( singleType ) ) { continue; }

                foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( singleType ) )
                {
                    var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                    tile?.ClearTileMesh( ownerKey, singleType );
                }

                if( singleType == TileMapType.QUEUED )
                {
                    _readOnlyActionableTileData.Value.ClearTileMap( TileMapType.QUEUED );
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
            ClearTileMeshesByType( TileMapType.QUEUED );

            _isShowingAttackableRange = false;
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
