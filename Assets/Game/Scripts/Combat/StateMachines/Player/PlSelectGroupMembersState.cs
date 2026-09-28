using Frontier.Entities;
using Frontier.Stage;
using System.Collections.Generic;
using System.Linq;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// PlSelectTileStateでOPT1入力によりグループ移動の登録者が0人から1人になった際に遷移する、
    /// グループ移動のメンバー選択ステートです。PlSelectTileStateを継承し、グリッドカーソル移動や
    /// OPT1による他キャラクターの登録・解除のみを受け付けます(ゴースト・移動経路のプレビューは行いません)。
    /// CONFIRM入力を受けると、登録済みのキャラクターを対象としてPlGroupMoveState(プレビュー・実行)へ遷移します。
    /// グループ移動への新たなキャラクターの登録は、このステートの中でのみ行えます。
    /// 登録中のキャラクターそれぞれの移動可能範囲を(攻撃関連の色を混ぜずに)表示し、画面上部に選択中である旨の案内を表示します。
    /// </summary>
    public class PlSelectGroupMembersState : PlSelectTileState
    {
        private enum TransitTag
        {
            GROUP_MOVE = 0,
        }

        // 移動可能範囲を描画済みのキャラクター(登録リストとの差分で描画・消去を行う)
        private readonly List<CharacterKey> _drawnRangeKeys = new List<CharacterKey>();

        public override bool Update()
        {
            // カーソル移動・文言更新・登録者の失格判定はPlSelectTileStateの実装をそのまま再利用する
            if( base.Update() ) { return true; }

            // PruneIneligibleRegistrationsによって登録者が0人になった場合や、
            // グループ移動の実行を終えて戻ってきた(登録が全て解除されている)場合は自動的に戻る
            if( _groupMoveRegistrationList.IsEmpty )
            {
                Back();
                return true;
            }

            // 失格判定等で登録内容が変化した場合にも、移動可能範囲の表示を追従させる
            SyncMoveRangeDisplay();

            return ( 0 <= TransitIndex );
        }

        public override object PauseState()
        {
            // PlGroupMoveStateは自前でプレビュー用の移動可能範囲を描画するため、ここでの表示・案内は一旦消去する
            ClearAllMoveRanges();
            _presenter.HideGuideMessage();

            return base.PauseState();
        }

        public override object ExitState()
        {
            // キャンセル等でタイル選択へ戻る場合は、登録していたキャラクターを全て解放する
            // (PlGroupMoveStateへの遷移はPauseStateとなるため、ここは呼ばれず登録は維持される)
            ClearAllMoveRanges();
            _presenter.HideGuideMessage();
            ClearAllRegistrations();

            return base.ExitState();
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            // 新規開始時、及びPlGroupMoveStateから戻った際(向こうで移動可能範囲の描画が消去されている)のいずれも、
            // 登録中の全キャラクターの移動可能範囲を描き直す
            ClearAllMoveRanges();
            SyncMoveRangeDisplay();

            _presenter.ShowGuideMessage( LocKey.UI_BATTLE_GUIDE_SELECT_GROUP_MEMBERS );
        }

        /// <summary>
        /// メンバー選択中は登録キャラクターの移動可能範囲のみを表示するため、カーソル上のキャラクターの
        /// 移動・攻撃範囲表示(ホバー範囲表示)は行わず、遷移元から引き継いだ表示も消去します
        /// </summary>
        protected override void RefreshHoveredRangeDisplay()
        {
            _hoveredRangeDisplay.Clear();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR, "MOVE",     CanAcceptDefault, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM,    "DECISION", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ),   0.0f, hashCode),
                (GuideIcon.CANCEL,     "BACK",     CanAcceptDefault, new AcceptContextInput( AcceptCancel ),    0.0f, hashCode),
                (GuideIcon.OPT1, _inputOpt1StrWrapper, CanAcceptOpt1, new AcceptContextInput( AcceptOpt1 ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 登録者が1人以上いる場合のみCONFIRM(プレビューへの遷移)を受け付けます
        /// </summary>
        protected override bool CanAcceptConfirm()
        {
            if( !CanAcceptDefault() || 0 <= TransitIndex ) { return false; }

            return !_groupMoveRegistrationList.IsEmpty;
        }

        /// <summary>
        /// 決定入力を受けた際、登録済みキャラクターのグループ移動プレビューへ遷移します
        /// </summary>
        protected override bool AcceptConfirm( InputContext context )
        {
            if( !AcceptConfirmCore( context ) ) { return false; }

            TransitState( ( int ) TransitTag.GROUP_MOVE );

            return true;
        }

        /// <summary>
        /// OPT1入力を受けた際、カーソル上のキャラクターの登録・解除を切り替えます。
        /// 登録者が0人になった場合は元のタイル選択ステートへ自動的に戻ります。
        /// </summary>
        protected override bool AcceptOpt1( InputContext context )
        {
            if( !AcceptOpt1Core( context ) ) { return false; }

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character ) { return false; }

            ToggleGroupMoveRegistration( character, out _ );

            if( _groupMoveRegistrationList.IsEmpty )
            {
                Back();
            }
            else
            {
                SyncMoveRangeDisplay();
            }

            return true;
        }

        /// <summary>
        /// 登録リストと描画済みリストの差分を取り、新たに登録されたキャラクターの移動可能範囲を描画し、
        /// 登録解除されたキャラクターの移動可能範囲を消去します
        /// </summary>
        private void SyncMoveRangeDisplay()
        {
            for( int i = _drawnRangeKeys.Count - 1; 0 <= i; --i )
            {
                if( _groupMoveRegistrationList.GetAll().Contains( _drawnRangeKeys[i] ) ) { continue; }

                ClearMoveRange( _drawnRangeKeys[i] );
                _drawnRangeKeys.RemoveAt( i );
            }

            foreach( var key in _groupMoveRegistrationList.GetAll() )
            {
                if( _drawnRangeKeys.Contains( key ) ) { continue; }

                Player character = _btlRtnCtrl.BtlCharaCdr.GetPlayer( key );
                if( null == character ) { continue; }

                int dprtIdx         = character.BattleParams.TmpParam.CurrentTileIndex;
                float dprtHeight    = _stageCtrl.GetTileStaticData( dprtIdx ).Height;
                var actionRangeCtrl = character.BattleLogic.ActionRangeCtrl;

                // 登録キャラクターごとに描画する。タイル毎にオーナーキー別のメッシュとしてY軸方向にずらして描画されるため、
                // 他キャラクターの範囲と重なっても埋もれず個別に視認できる
                actionRangeCtrl.SetupActionableRangeData( dprtIdx, dprtHeight );
                actionRangeCtrl.DrawMoveOnlyRange();

                _drawnRangeKeys.Add( key );
            }
        }

        /// <summary>
        /// 描画済みの全キャラクターの移動可能範囲を消去します
        /// </summary>
        private void ClearAllMoveRanges()
        {
            foreach( var key in _drawnRangeKeys )
            {
                ClearMoveRange( key );
            }

            _drawnRangeKeys.Clear();
        }

        private void ClearMoveRange( CharacterKey key )
        {
            _btlRtnCtrl.BtlCharaCdr.GetPlayer( key )?.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.MOVEABLE );
        }

    }
}
