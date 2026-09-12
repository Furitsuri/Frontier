using Frontier.StateMachine;
using Frontier.TroopEdit;
using Frontier.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.FormTroop
{
    /// <summary>
    /// 「解雇」選択時に表示する自軍メンバー一覧グリッド画面。
    /// グリッド表示・カーソル移動・キャラクターのライフサイクルは、FieldSceneの部隊編集画面
    /// (TroopEditHandler)と共通のTroopGridControllerに委譲する。雇用画面(RecruitRootState)と
    /// 同様、チェックマークのトグルで複数メンバーを選択し、COMPLETE操作でまとめて解雇できる。
    /// </summary>
    public sealed class RecruitDismissState : RecruitPhaseStateBase
    {
        private enum RecruitDismissTransitTag
        {
            COMPLETE = 0,
            GREETING,
        }

        [Inject] private UserDomain _userDomain = null;
        [Inject] private GeneralHeaderPresenter _headerPresenter = null;

        private TroopEditPresenter _troopEditPresenter      = null;
        private CharacterParameterPresenter _paramPresenter = null;
        private TroopGridController _gridController         = null;
        private List<int> _rewardAnimas       = new List<int>();
        private List<bool> _dismissChecked    = new List<bool>();

        private bool _isExistDismissChecked = false;
        private string[] _inputConfirmStrings;
        private InputCodeStringWrapper _inputConfirmStrWrapper = null;

        public override void Init( object context )
        {
            base.Init( context );

            // CONFIRMアイコンの文字列を設定(雇用画面と同様、チェック状態に応じて切り替える)
            _inputConfirmStrings = new string[]
            {
                "DISMISS\nCHECK",   // 解雇チェック
                "CANCEL\nCHECK",    // チェック取消
            };
            _inputConfirmStrWrapper = new InputCodeStringWrapper( _inputConfirmStrings[0] );

            LazyInject.GetOrCreate( ref _troopEditPresenter, () => _hierarchyBld.InstantiateWithDiContainer<TroopEditPresenter>( false ) );
            _troopEditPresenter.Init();
            _headerPresenter.SetStateTitle( LocKey.UI_CMD_DISMISS );

            LazyInject.GetOrCreate( ref _paramPresenter, () => _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>(
                new object[] { _troopEditPresenter.CharacterParamUI, false }, false ) );
            _paramPresenter.Init();

            LazyInject.GetOrCreate( ref _gridController, () => _hierarchyBld.InstantiateWithDiContainer<TroopGridController>( false ) );
            _gridController.Init( _troopEditPresenter, _paramPresenter );

            InitializeRewardAnimas();
            InitializeDismissChecks();
            BuildRoster();

            _isExistDismissChecked = false;

            // 解雇可能なメンバーが一人も居ない場合(自軍が1人になる解雇は許可しないため、
            // 残り1人の場合も含む)のみ、店主の会話クッション画面を経由する(会話を閉じると
            // RestartState()でこの画面の表示に戻る)。クッション画面表示中はカーソル・
            // パラメータパネルを隠し、表示するメッセージはcontext経由で渡す。
            if( _userDomain.Members.Count <= 1 )
            {
                _gridController.HideSelectionDisplay();
                SetSendTransitionContext( new TalkWindowCushionContext( LocKey.UI_TALK_SHOPKEEPER_NAME, LocKey.UI_TALK_DISMISS_NONE_AVAILABLE ) );
                TransitState( ( int ) RecruitDismissTransitTag.GREETING );
            }
        }

        /// <summary>
        /// 会話クッション画面等の子Stateから戻ってきた際に呼ばれます。
        /// 子State表示中に隠していたカーソル・選択中キャラクターのパラメータパネルを再表示します。
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();

            _gridController.ShowSelectionDisplay();
        }

        public override bool Update()
        {
            // 解雇確定によって対象が0体になった場合はCONFIRMアイコンの文字列更新をスキップする
            if( _gridController.SpawnedCharacters.Count > 0 )
            {
                _inputConfirmStrWrapper.Explanation = _dismissChecked[_gridController.SelectedIndex] ? _inputConfirmStrings[1] : _inputConfirmStrings[0];
            }

            return ( 0 <= TransitIndex );
        }

        public override object ExitState()
        {
            _gridController.Close();
            _headerPresenter.ClearStateTitle();

            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR, "SELECT",   CanAcceptDefault,  new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,    _inputConfirmStrWrapper,      CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ),   0.0f, hashCode),
               (GuideIcon.CANCEL,     "BACK",     CanAcceptDefault,  new AcceptContextInput( AcceptCancel ),    0.0f, hashCode),
               (GuideIcon.OPT2,       "COMPLETE", CanAcceptOptional, new AcceptContextInput( AcceptOpt2 ),      0.0f, hashCode)
            );
        }

        protected override bool CanAcceptConfirm()
        {
            if( _gridController.SpawnedCharacters.Count == 0 ) { return false; }

            int index = _gridController.SelectedIndex;

            // 既にチェック済みのメンバーは常にチェックを解除できる
            if( _dismissChecked[index] ) { return true; }

            // 新たにチェックする場合、自軍が1人になってしまう解雇は許可しない
            int checkedCount = _dismissChecked.Count( c => c );
            return ( checkedCount + 1 ) < _userDomain.Members.Count;
        }

        protected override bool CanAcceptOptional()
        {
            return _isExistDismissChecked;   // 解雇チェック済みのメンバーが一人もいない場合は完了できない
        }

        protected override bool AcceptDirection( InputContext context )
        {
            return _gridController.MoveSelection( context.Cursor );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            int index = _gridController.SelectedIndex;
            _dismissChecked[index] = !_dismissChecked[index];

            _troopEditPresenter.SetChecked( index, _dismissChecked[index] );
            _isExistDismissChecked = IsExistDismissChecked();

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            Back();

            return true;
        }

        protected override bool AcceptOpt2( InputContext context )
        {
            if( !base.AcceptOpt2( context ) ) { return false; }

            // 確認画面の会話文言(単数/複数)を選ぶための、解雇チェック済み人数を渡す
            int checkedCount = _dismissChecked.Count( c => c );
            SetSendTransitionContext( checkedCount );

            // 解雇完了確認ステートへ遷移
            TransitState( ( int ) RecruitDismissTransitTag.COMPLETE );

            return true;
        }

        /// <summary>
        /// 解雇完了確認ステートでYesが選択された際に呼ばれます。解雇チェック済みのメンバーを
        /// まとめて解雇し、報酬アニマを加算した上で表示から取り除きます(RecruitSceneは終了しない)。
        /// </summary>
        public void CommitDismissal()
        {
            for( int i = _dismissChecked.Count - 1; i >= 0; --i )
            {
                if( !_dismissChecked[i] ) { continue; }

                _userDomain.AddAnima( _rewardAnimas[i] );
                _userDomain.DismissMember( i );

                _rewardAnimas.RemoveAt( i );
                _dismissChecked.RemoveAt( i );

                _gridController.RemoveCharacterAt( i );
            }

            for( int i = 0; i < _rewardAnimas.Count; ++i )
            {
                _troopEditPresenter.SetRewardAnima( i, _rewardAnimas[i] );
                _troopEditPresenter.SetChecked( i, _dismissChecked[i] );
            }

            _isExistDismissChecked = IsExistDismissChecked();
        }

        private bool IsExistDismissChecked()
        {
            return _dismissChecked.Contains( true );
        }

        /// <summary>
        /// 解雇報酬アニマ額を、このState突入時に一度だけ算出します。以後は解雇確定によって
        /// リストから該当分を取り除く場合を除き、再計算しません。
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
        /// 解雇チェック状態を、このState突入時に一度だけ全て未チェックへ初期化します。
        /// </summary>
        private void InitializeDismissChecks()
        {
            _dismissChecked.Clear();
            for( int i = 0; i < _userDomain.Members.Count; ++i )
            {
                _dismissChecked.Add( false );
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
