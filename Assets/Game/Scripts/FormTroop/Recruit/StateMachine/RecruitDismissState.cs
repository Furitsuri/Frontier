using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Constants;

namespace Frontier.FormTroop
{
    /// <summary>
    /// 「解雇」選択時に表示する自軍メンバー一覧グリッド画面。
    /// グリッド表示・カーソル移動・ステータス確認等、雇用画面(RecruitEmployState)と共通の処理は
    /// RecruitGridStateBaseに委譲する。チェックマークのトグルで複数メンバーを選択し、
    /// COMPLETE操作でまとめて解雇できる。
    /// </summary>
    public sealed class RecruitDismissState : RecruitGridStateBase
    {
        private List<int> _rewardAnimas       = new List<int>();
        private List<bool> _dismissChecked    = new List<bool>();

        private bool _isExistDismissChecked = false;

        public override void Init( object context )
        {
            base.Init( context );

            SetupGrid( LocKey.UI_CMD_DISMISS, new[]
            {
                "DISMISS\nCHECK",   // 解雇チェック
                "CANCEL\nCHECK",    // チェック取消
            } );

            InitializeRewardAnimas();
            InitializeDismissChecks();
            BuildRoster();

            _isExistDismissChecked = false;

            // 解雇可能なメンバーが一人も居ない場合(自軍が1人になる解雇は許可しないため、
            // 残り1人の場合も含む)のみ、店主の会話クッション画面を経由する
            ShowNoneAvailableCushionIfNeeded( _userDomain.Members.Count <= 1, LocKey.UI_TALK_DISMISS_NONE_AVAILABLE );
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

        /// <summary>
        /// 選択中キャラクターが解雇チェック済みかどうかを返します(CONFIRMアイコン文言の切り替えに使用)。
        /// </summary>
        protected override bool IsSelectedToggled()
        {
            return _dismissChecked[_gridController.SelectedIndex];
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

        /// <summary>
        /// 解雇完了確認画面へ渡す、解雇チェック済み人数を返します。
        /// </summary>
        protected override int GetToggledCount()
        {
            return _dismissChecked.Count( c => c );
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
