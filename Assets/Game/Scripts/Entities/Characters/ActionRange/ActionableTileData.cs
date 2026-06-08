using Frontier.Stage;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Frontier.Entities
{
    /// <summary>
    /// アクション(移動や攻撃など)可能なタイルの情報。
    /// TileMapType enum でインデックスされた配列でタイルマップを管理します。
    /// </summary>
    public class ActionableTileData
    {
        private const int TILE_MAP_COUNT = 4;

        private readonly Dictionary<int, TileDynamicData>[] _tileMaps
            = new Dictionary<int, TileDynamicData>[TILE_MAP_COUNT]
            {
                new Dictionary<int, TileDynamicData>(),  // MOVEABLE
                new Dictionary<int, TileDynamicData>(),  // ATTACKABLE
                new Dictionary<int, TileDynamicData>(),  // TARGETABLE
                new Dictionary<int, TileDynamicData>(),  // QUEUED
            };

        private List<int> _attackTargetTileIndicies = new List<int>();

        // 後方互換プロパティ
        public Dictionary<int, TileDynamicData> MoveableTileMap   => GetTileMap( TileMapType.MOVEABLE );
        public Dictionary<int, TileDynamicData> AttackableTileMap => GetTileMap( TileMapType.ATTACKABLE );
        public Dictionary<int, TileDynamicData> TargetableTileMap => GetTileMap( TileMapType.TARGETABLE );
        public ReadOnlyCollection<int> RefAttackTargetTileIndicies => new ReadOnlyCollection<int>( _attackTargetTileIndicies );

        /// <summary>
        /// 指定タイプのタイルマップを返します（単一ビット値のみ有効）
        /// </summary>
        public Dictionary<int, TileDynamicData> GetTileMap( TileMapType type )
            => _tileMaps[TileMapTypeToIndex( type )];

        public void Init()
        {
            for( int i = 0; i < TILE_MAP_COUNT; ++i )
            {
                _tileMaps[i].Clear();
            }
            _attackTargetTileIndicies.Clear();
        }

        public void Dispose()
        {
            _attackTargetTileIndicies.Clear();
            _attackTargetTileIndicies = null;
            for( int i = 0; i < TILE_MAP_COUNT; ++i )
            {
                _tileMaps[i]?.Clear();
            }
        }

        public void AddMoveableTile( int index, TileDynamicData addTileData )
        {
            _tileMaps[TileMapTypeToIndex( TileMapType.MOVEABLE )][index] = addTileData;
        }

        public void AddAttackableTile( int index, TileDynamicData addTileDData )
        {
            _tileMaps[TileMapTypeToIndex( TileMapType.ATTACKABLE )][index] = addTileDData;
        }

        public void AddTargetableTile( int index, TileDynamicData addTileData )
        {
            _tileMaps[TileMapTypeToIndex( TileMapType.TARGETABLE )][index] = addTileData;
        }

        /// <summary>予約済み表示タイルを登録します（TileDynamicData は不要のため null）</summary>
        public void AddQueuedTile( int index )
        {
            _tileMaps[TileMapTypeToIndex( TileMapType.QUEUED )][index] = null;
        }

        /// <summary>指定タイプのタイルマップをクリアします（単一ビット値のみ有効）</summary>
        public void ClearTileMap( TileMapType type )
        {
            _tileMaps[TileMapTypeToIndex( type )].Clear();
        }

        public void AddAttackTargetTileIndex( int index )
        {
            _attackTargetTileIndicies.Add( index );
        }

        public void ClearTargetableTile()
        {
            _tileMaps[TileMapTypeToIndex( TileMapType.TARGETABLE )].Clear();
        }

        public void ClearAttackTargetTileIndicies()
        {
            _attackTargetTileIndicies.Clear();
        }

        public void DeleteTileMap( TileMapType type, int index )
        {
            _tileMaps[TileMapTypeToIndex( type )].Remove( index );
        }

        public void DeleteAttackTargetTileIndex( int index )
        {
            _attackTargetTileIndicies.Remove( index );
        }

        public bool IsEmpty()
        {
            return MoveableTileMap.Count <= 0 && AttackableTileMap.Count <= 0;
        }

        public TileDynamicData GetMoveableTile( int index )
        {
            var map = GetTileMap( TileMapType.MOVEABLE );
            return map.TryGetValue( index, out var data ) ? data : null;
        }

        public TileDynamicData GetAttackableTile( int index )
        {
            var map = GetTileMap( TileMapType.ATTACKABLE );
            return map.TryGetValue( index, out var data ) ? data : null;
        }

        /// <summary>
        /// TileMapType の単一ビット値を配列インデックスに変換します
        /// </summary>
        public static int TileMapTypeToIndex( TileMapType type )
        {
            int v = (int)type;
            int idx = 0;
            while( v > 1 ) { v >>= 1; ++idx; }
            return idx;
        }
    }
}
