using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// アクション(移動や攻撃など)可能なタイルの情報
    /// </summary>
    public class ActionableTileMap
    {
        private Dictionary<int, TileDynamicData> _moveableTileMap      = new Dictionary<int, TileDynamicData>();
        private Dictionary<int, TileDynamicData> _attackableTileMap    = new Dictionary<int, TileDynamicData>();
        public Dictionary<int, TileDynamicData> MoveableTileMap { get { return _moveableTileMap; } }
        public Dictionary<int, TileDynamicData> AttackableTileMap { get { return _attackableTileMap; } }

        public void Init()
        {
            _moveableTileMap.Clear();
            _attackableTileMap.Clear();
        }

        public void AddMoveableTile( int index, TileDynamicData addTileData )
        {
            _moveableTileMap[index] = addTileData;
        }

        public void AddAttackableTile( int index, TileDynamicData addTileDData )
        {
            _attackableTileMap[index] = addTileDData;
        }

        public TileDynamicData GetMoveableTile(int index )
        {
            if( !_moveableTileMap.ContainsKey( index ) ) { return null; }

            return _moveableTileMap[index];
        }

        public TileDynamicData GetAttackableTile(int index)
        {
            if( !_attackableTileMap.ContainsKey( index ) ) { return null; }

            return _attackableTileMap[index];
        }

        public bool IsEmpty()
        {
            return _moveableTileMap.Count <= 0 && _attackableTileMap.Count <= 0;
        }
    }
}
