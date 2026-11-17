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

        // 予約しているキャラクターと、予約した攻撃、その範囲を示す予兆の表示(範囲が無い場合はnull)
        private readonly Dictionary<Character, (AttackOption option, AttackTelegraphAreaView view)> _reservations = new Dictionary<Character, (AttackOption, AttackTelegraphAreaView)>();

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

            _reservations[owner] = (option, ShowTelegraph( in option ));
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

            HideTelegraph( reservation.view );
            _reservations.Remove( owner );
        }

        /// <summary>
        /// 全ての予約を取り消し、予兆の表示を消去します
        /// </summary>
        public void Clear()
        {
            foreach( var reservation in _reservations.Values )
            {
                HideTelegraph( reservation.view );
            }
            _reservations.Clear();
        }

        /// <summary>
        /// 予約した攻撃の範囲を予兆として表示します。予約ごとに外周を縁取った範囲を1つ表示するため、範囲が重なっても予約ごとの形が判別できます
        /// </summary>
        /// <returns>生成した予兆の表示(範囲が無い場合はnull)</returns>
        private AttackTelegraphAreaView ShowTelegraph( in AttackOption option )
        {
            if( option.AffectedTileIndices == null || option.AffectedTileIndices.Count <= 0 ) { return null; }

            var view = _hierarchyBld.CreateComponentAndOrganize<AttackTelegraphAreaView>( _prefabReg.AttackTelegraphAreaPrefab, true );
            view.Show( _stageDataProvider.CurrentData, option.AffectedTileIndices );

            return view;
        }

        private void HideTelegraph( AttackTelegraphAreaView view )
        {
            if( view != null ) { view.Remove(); }
        }
    }
}
