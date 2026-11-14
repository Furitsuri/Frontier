using Frontier.Registries;
using Frontier.Stage;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Entities.Ai
{
    /// <summary>
    /// NPCが次の自軍のフェーズで使用を予約した攻撃(予兆を伴うスキルによる攻撃)を保持し、その範囲を予兆としてステージ上に表示します。
    /// 予約は自軍のフェーズの終了時(AttackTelegraphPlanner)に登録され、次の自軍のフェーズで実行・取り消しされるか、
    /// 予約したキャラクターが撃破されるまで表示され続けます。
    /// </summary>
    public class AttackTelegraphRegistry
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private PrefabRegistry _prefabReg              = null;
        [Inject] private IStageDataProvider _stageDataProvider  = null;

        // 予約しているキャラクターと、予約した時点のCharacterKey(予兆の表示・消去に用いる)、予約した攻撃
        private readonly Dictionary<Character, (CharacterKey key, AttackOption option)> _reservations = new Dictionary<Character, (CharacterKey, AttackOption)>();

        /// <summary>
        /// 予約しているキャラクターの数
        /// </summary>
        public int Count => _reservations.Count;

        /// <summary>
        /// 攻撃を予約し、その範囲を予兆として表示します。既に予約している場合は置き換えます
        /// </summary>
        /// <param name="owner">予約するキャラクター</param>
        /// <param name="option">予約する攻撃</param>
        public void Reserve( Character owner, in AttackOption option )
        {
            Remove( owner );

            var ownerKey = owner.GetCharacterKey();
            _reservations[owner] = (ownerKey, option);
            ShowTelegraph( ownerKey, in option );
        }

        /// <summary>
        /// 予約している攻撃を取得します
        /// </summary>
        /// <returns>予約しているか否か</returns>
        public bool TryGet( Character owner, out AttackOption option )
        {
            option = default;
            if( owner == null || !_reservations.TryGetValue( owner, out var reservation ) ) { return false; }

            option = reservation.option;
            return true;
        }

        /// <summary>
        /// 予約しているかを取得します
        /// </summary>
        public bool Has( Character owner )
        {
            return owner != null && _reservations.ContainsKey( owner );
        }

        /// <summary>
        /// 予約を取り消し、予兆の表示を消去します(予約していない場合は何もしません)
        /// </summary>
        public void Remove( Character owner )
        {
            if( owner == null || !_reservations.TryGetValue( owner, out var reservation ) ) { return; }

            HideTelegraph( reservation.key, in reservation.option );
            _reservations.Remove( owner );
        }

        /// <summary>
        /// 全ての予約を取り消し、予兆の表示を消去します
        /// </summary>
        public void Clear()
        {
            foreach( var reservation in _reservations.Values )
            {
                HideTelegraph( reservation.key, in reservation.option );
            }
            _reservations.Clear();
        }

        private void ShowTelegraph( CharacterKey ownerKey, in AttackOption option )
        {
            if( option.AffectedTileIndices == null ) { return; }

            foreach( int tileIndex in option.AffectedTileIndices )
            {
                _stageDataProvider.CurrentData.GetTile( tileIndex )?.AddTelegraph( ownerKey, in TileColors.Colors[( int ) MeshType.TELEGRAPH], CreateTileMesh );
            }
        }

        private void HideTelegraph( CharacterKey ownerKey, in AttackOption option )
        {
            if( option.AffectedTileIndices == null ) { return; }

            foreach( int tileIndex in option.AffectedTileIndices )
            {
                _stageDataProvider.CurrentData.GetTile( tileIndex )?.RemoveTelegraph( ownerKey );
            }
        }

        private TileMesh CreateTileMesh()
        {
            return _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true );
        }
    }
}
