using Frontier.Combat;
using Frontier.Registries;
using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Entities.Ai
{
    /// <summary>
    /// NPCが次の自軍のフェーズで使用を予約した攻撃(予兆を伴うスキルによる攻撃)を保持し、その範囲を予兆としてステージ上に表示します。
    /// 予約は自軍のフェーズの終了時(AttackTelegraphPlanner)に登録され、次の自軍のフェーズで実行・取り消しされるか、
    /// 予約したキャラクターが撃破されるまで表示され続けます。
    ///
    /// 移動を伴うスキルの場合は、着地先のタイルを予約したキャラクター以外が立てないよう予約し(TileDataHandler.ReserveTile)、
    /// 着地先にそのキャラクターのゴースト(これからの位置を示す半透明の姿)を表示します(ゴーストはキャラクターが保持するものを用い、表示・非表示のみを切り替えます)。
    /// 着地先を他のキャラクターで塞いで射程を縮める(予兆の通りに実行させない)ことが出来ないようにするためです。
    /// </summary>
    public class AttackTelegraphRegistry
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private PrefabRegistry _prefabReg              = null;
        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private DiContainer _container                 = null;

        // MEMO : StageControllerはBattleRoutineControllerの初期化時に生成・バインドされ、このクラスはそれより前に生成されるため注入出来ない。
        //        予約を行う時点(戦闘の開始後)にコンテナから取得する
        private StageController _stageCtrlCache = null;
        private StageController _stageCtrl => _stageCtrlCache ??= _container.Resolve<StageController>();

        private struct Reservation
        {
            public AttackOption Option;
            public AttackTelegraphAreaView AreaView;    // 範囲を示す予兆の表示(範囲が無い場合はnull)
        }

        // 予約しているキャラクターと、その予約
        private readonly Dictionary<Character, Reservation> _reservations = new Dictionary<Character, Reservation>();

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

            _reservations[owner] = new Reservation
            {
                Option          = option,
                AreaView        = ShowTelegraph( in option ),
            };
            ReserveLanding( owner, in option );
        }

        /// <summary>
        /// 予約している攻撃を取得します
        /// </summary>
        /// <returns>予約しているか否か</returns>
        public bool TryGet( Character owner, out AttackOption option )
        {
            option = default;
            if( owner == null || !_reservations.TryGetValue( owner, out var reservation ) ) { return false; }

            option = reservation.Option;
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

            Release( owner, in reservation );
            _reservations.Remove( owner );
        }

        /// <summary>
        /// 全ての予約を取り消し、予兆の表示を消去します
        /// </summary>
        public void Clear()
        {
            foreach( var reservation in _reservations )
            {
                Release( reservation.Key, reservation.Value );
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

        /// <summary>
        /// 移動を伴うスキルの場合、着地先のタイルを予約したキャラクター以外が立てないよう予約し、着地先にゴーストを表示します
        /// </summary>
        private void ReserveLanding( Character owner, in AttackOption option )
        {
            if( option.LandingTileIndex < 0 ) { return; }

            _stageCtrl.TileDataHdlr().ReserveTile( option.LandingTileIndex, owner.GetCharacterKey() );

            var landingTile = _stageCtrl.GetTileStaticData( option.LandingTileIndex );
            owner.ShowGhostAt( option.LandingTileIndex, landingTile.CharaStandPos, Quaternion.Euler( 0f, StageDirectionConverter.DirectionAngles[( int ) option.Direction], 0f ) );
        }

        /// <summary>
        /// 予約の表示を消去し、着地先のタイルの予約を解除します
        /// </summary>
        private void Release( Character owner, in Reservation reservation )
        {
            if( reservation.AreaView != null ) { reservation.AreaView.Remove(); }
            if( owner != null ) { owner.SetGhostActive( false ); }
            if( 0 <= reservation.Option.LandingTileIndex ) { _stageCtrl.TileDataHdlr().ReleaseTile( reservation.Option.LandingTileIndex ); }
        }
    }
}
