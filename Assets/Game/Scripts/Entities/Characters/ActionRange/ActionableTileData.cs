using Frontier.Stage;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// アクション(移動や攻撃など)可能なタイルの情報
    /// </summary>
    public class ActionableTileData
    {
        private Dictionary<int, TileDynamicData> _moveableTileMap    = new Dictionary<int, TileDynamicData>();
        private Dictionary<int, TileDynamicData> _attackableTileMap  = new Dictionary<int, TileDynamicData>();
        private Dictionary<int, TileDynamicData> _targetableTileMap  = new Dictionary<int, TileDynamicData>();
        private List<int> _attackTargetTileIndicies                  = new List<int>();
        public Dictionary<int, TileDynamicData> MoveableTileMap { get { return _moveableTileMap; } }
        public Dictionary<int, TileDynamicData> AttackableTileMap { get { return _attackableTileMap; } }
        public Dictionary<int, TileDynamicData> TargetableTileMap { get { return _targetableTileMap; } }
        public ReadOnlyCollection<int> RefAttackTargetTileIndicies { get { return new ReadOnlyCollection<int>( _attackTargetTileIndicies ); } }

        public void Init()
        {
            _moveableTileMap.Clear();
            _attackableTileMap.Clear();
            _targetableTileMap.Clear();
            _attackTargetTileIndicies.Clear();
        }

        public void Dispose()
        {
            _attackTargetTileIndicies.Clear();
            _targetableTileMap.Clear();
            _attackableTileMap.Clear();
            _moveableTileMap.Clear();

            _attackTargetTileIndicies   = null;
            _targetableTileMap          = null;
            _attackableTileMap          = null;
            _moveableTileMap            = null;
        }

        public void AddMoveableTile( int index, TileDynamicData addTileData )
        {
            _moveableTileMap[index] = addTileData;
        }

        public void AddAttackableTile( int index, TileDynamicData addTileDData )
        {
            _attackableTileMap[index] = addTileDData;
        }

        public void AddTargetableTile( int index, TileDynamicData addTileData )
        {
            _targetableTileMap[index] = addTileData;
        }

        public void AddAttackTargetTileIndex( int index )
        {
            Debug.Assert( _attackableTileMap[index].CharaKey.IsValid(), "Attackable tile data is null" );

            _attackTargetTileIndicies.Add( index );
        }

        public void ClearTargetableTile()
        {
            _targetableTileMap.Clear();
        }

        public void ClearAttackTargetTileIndicies()
        {
            _attackTargetTileIndicies.Clear();
        }

        public bool IsEmpty()
        {
            return _moveableTileMap.Count <= 0 && _attackableTileMap.Count <= 0;
        }

        public TileDynamicData GetMoveableTile( int index )
        {
            if( !_moveableTileMap.ContainsKey( index ) ) { return null; }

            return _moveableTileMap[index];
        }

        public TileDynamicData GetAttackableTile( int index )
        {
            if( !_attackableTileMap.ContainsKey( index ) ) { return null; }

            return _attackableTileMap[index];
        }
    }
}