using Frontier.Entities;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// PlSelectTileStateでOPT1入力によりグループ移動の登録者が0人から1人になった際に遷移する、
    /// グループ移動のメンバー選択ステートです。PlSelectTileStateを継承し、グリッドカーソル移動や
    /// OPT1による他キャラクターの登録・解除のみを受け付けます(ゴースト・移動経路のプレビューは行いません)。
    /// CONFIRM入力を受けると、登録済みのキャラクターを対象としてPlGroupMoveState(プレビュー・実行)へ遷移します。
    /// グループ移動への新たなキャラクターの登録は、このステートの中でのみ行えます。
    /// </summary>
    public class PlSelectGroupMembersState : PlSelectTileState
    {
        private enum TransitTag
        {
            GROUP_MOVE = 0,
        }

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

            return ( 0 <= TransitIndex );
        }

        public override object ExitState()
        {
            // キャンセル等でタイル選択へ戻る場合は、登録していたキャラクターを全て解放する
            // (PlGroupMoveStateへの遷移はPauseStateとなるため、ここは呼ばれず登録は維持される)
            ClearAllRegistrations();

            return base.ExitState();
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

            return true;
        }

        /// <summary>
        /// グループ移動の登録キャラクターを全て解放します(マテリアルを元に戻した上で登録リストをクリアします)
        /// </summary>
        private void ClearAllRegistrations()
        {
            foreach( var key in _groupMoveRegistrationList.GetAll() )
            {
                _btlRtnCtrl.BtlCharaCdr.GetPlayer( key )?.RestoreMaterialsOriginalColor();
            }

            _groupMoveRegistrationList.Clear();
        }
    }
}
