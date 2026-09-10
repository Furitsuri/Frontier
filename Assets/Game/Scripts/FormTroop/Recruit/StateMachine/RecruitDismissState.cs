using Frontier.StateMachine;
using Frontier.TroopEdit;
using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.FormTroop
{
    /// <summary>
    /// 「解雇」選択時に表示する自軍メンバー一覧グリッド画面。
    /// グリッド表示・カーソル移動・キャラクターのライフサイクルは、FieldSceneの部隊編集画面
    /// (TroopEditHandler)と共通のTroopGridControllerに委譲する。このStateが持つのは
    /// 解雇報酬(_rewardAnimas)の管理と、確定/キャンセル時の遷移(確認Stateへの遷移・Back())のみ。
    /// </summary>
    public sealed class RecruitDismissState : RecruitPhaseStateBase
    {
        private enum RecruitDismissTransitTag
        {
            CONFIRM_DISMISS = 0,
            GREETING,
        }

        [Inject] private UserDomain _userDomain = null;
        [Inject] private GeneralHeaderPresenter _headerPresenter = null;

        private TroopEditPresenter _troopEditPresenter      = null;
        private CharacterParameterPresenter _paramPresenter = null;
        private TroopGridController _gridController         = null;
        private List<int> _rewardAnimas            = new List<int>();
        private int _pendingDismissIndex  = -1;  // 確認Stateからリクエストされた解雇対象(-1は未リクエスト)

        public override void Init( object context )
        {
            base.Init( context );

            LazyInject.GetOrCreate( ref _troopEditPresenter, () => _hierarchyBld.InstantiateWithDiContainer<TroopEditPresenter>( false ) );
            _troopEditPresenter.Init();
            _headerPresenter.SetStateTitle( LocKey.UI_CMD_DISMISS );

            LazyInject.GetOrCreate( ref _paramPresenter, () => _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>(
                new object[] { _troopEditPresenter.CharacterParamUI, false }, false ) );
            _paramPresenter.Init();

            LazyInject.GetOrCreate( ref _gridController, () => _hierarchyBld.InstantiateWithDiContainer<TroopGridController>( false ) );
            _gridController.Init( _troopEditPresenter, _paramPresenter );

            _pendingDismissIndex = -1;

            InitializeRewardAnimas();
            BuildRoster();

            // 突入時は必ず店主の会話クッション画面を経由する(会話を閉じるとこの画面の操作に移れる)
            TransitState( ( int ) RecruitDismissTransitTag.GREETING );
        }

        /// <summary>
        /// 解雇可能なメンバーが1体でも残っているか(店主の会話クッション画面が表示メッセージを選ぶ際に使用)。
        /// 自軍が1人になる解雇は許可しないため、2人以上いる場合のみ解雇可能とする。
        /// </summary>
        public bool HasDismissableMembers => 1 < _userDomain.Members.Count;

        /// <summary>
        /// カーソル・選択中キャラクターのパラメータパネルを一時的に隠します(会話クッション画面表示中)。
        /// </summary>
        public void HideGridSelectionDisplay() => _gridController.HideSelectionDisplay();

        /// <summary>
        /// HideGridSelectionDisplay()で隠したカーソル・パラメータパネルを再表示します。
        /// </summary>
        public void ShowGridSelectionDisplay() => _gridController.ShowSelectionDisplay();

        public override object ExitState()
        {
            _gridController.Close();
            _headerPresenter.ClearStateTitle();

            return base.ExitState();
        }

        /// <summary>
        /// 解雇確認画面から戻ってきた際に呼ばれます。RequestDismiss()でリクエストが来ていれば
        /// (YES)、実際の解雇処理を行い、解雇された1体だけをグリッドから取り除きます。
        /// キャンセル(NO)の場合は表示に変更が無いため何もしません
        /// (毎回グリッドを再構築すると、残っているキャラクター達の再生中アニメーションが
        /// 途切れてしまうため、実際に変化があった場合のみ更新するようにしています)。
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();

            if( 0 <= _pendingDismissIndex )
            {
                int reward = _rewardAnimas[_pendingDismissIndex];
                _userDomain.AddAnima( reward );
                _userDomain.DismissMember( _pendingDismissIndex );
                _rewardAnimas.RemoveAt( _pendingDismissIndex );

                _gridController.RemoveCharacterAt( _pendingDismissIndex );

                for( int i = 0; i < _rewardAnimas.Count; ++i )
                {
                    _troopEditPresenter.SetRewardAnima( i, _rewardAnimas[i] );
                }

                _pendingDismissIndex = -1;
            }
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR, "SELECT",  CanAcceptDefault, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,    "CONFIRM", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ),   0.0f, hashCode),
               (GuideIcon.CANCEL,     "BACK",    CanAcceptDefault, new AcceptContextInput( AcceptCancel ),    0.0f, hashCode)
            );
        }

        protected override bool CanAcceptConfirm()
        {
            // 自軍が1人になる解雇は許可しない
            return _gridController.SpawnedCharacters.Count > 0 && 1 < _userDomain.Members.Count;
        }

        protected override bool AcceptDirection( InputContext context )
        {
            return _gridController.MoveSelection( context.Cursor );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            // 解雇対象・報酬額を確認Stateへ渡す(解雇確定はRestartState側で行う)
            SetSendTransitionContext( _gridController.SelectedIndex );
            TransitState( ( int ) RecruitDismissTransitTag.CONFIRM_DISMISS );

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            Back();

            return true;
        }

        /// <summary>
        /// 確認Stateから、指定インデックスのメンバーの解雇を要求します。
        /// 実際の解雇処理(除名+アニマ加算)はRestartState()で行います。
        /// </summary>
        public void RequestDismiss( int index )
        {
            _pendingDismissIndex = index;
        }

        /// <summary>
        /// 選択中の報酬アニマ量を返します(確認画面のメッセージ表示等に使う想定)。
        /// </summary>
        public int GetRewardAnima( int index ) => _rewardAnimas[index];

        /// <summary>
        /// 解雇報酬アニマ額を、このState突入時に一度だけ算出します。以後は解雇確定によって
        /// リストから該当分を取り除く場合を除き、再計算しません(RestartState参照)。
        /// </summary>
        private void InitializeRewardAnimas()
        {
            _rewardAnimas.Clear();
            for( int i = 0; i < _userDomain.Members.Count; ++i )
            {
                // MEMO : 解雇報酬の仕様は未確定のため、暫定的に1〜20のランダム値とする
                _rewardAnimas.Add( Random.Range( 1, 21 ) );
            }
        }

        /// <summary>
        /// UserDomain.Membersから表示用キャラクターを生成し、グリッド・ステータスパネル・
        /// 報酬アニマ表示を初期表示します(このState突入時に一度だけ呼ばれる)。
        /// </summary>
        private void BuildRoster()
        {
            var members = _userDomain.Members;

            // 雇用候補キャラクター(CharacterCandidate)が同シーン内でCHARACTER_SELECTION_OFFSET_Zの
            // オフスクリーン待機位置を使い続けているため、座標が重ならないよう専用のZ座標を使う
            _gridController.Show( members, DISMISS_CHARACTER_OFFSET_Z );

            for( int i = 0; i < _rewardAnimas.Count; ++i )
            {
                _troopEditPresenter.SetRewardAnima( i, _rewardAnimas[i] );
            }
        }
    }
}
