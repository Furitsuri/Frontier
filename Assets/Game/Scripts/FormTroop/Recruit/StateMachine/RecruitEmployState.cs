using Frontier.Entities;
using Frontier.Tutorial;
using System.Collections.Generic;
using System.Linq;

namespace Frontier.FormTroop
{
    /// <summary>
    /// 「雇用」選択時に表示する雇用候補一覧グリッド画面。
    /// グリッド表示・カーソル移動・ステータス確認等、解雇画面(RecruitDismissState)と共通の処理は
    /// RecruitGridStateBaseに委譲する。雇用候補キャラクター(CharacterCandidate)はRecruitTopMenuState
    /// が生成・所有・破棄するため、このStateはShowExisting()で表示を借りるだけで、破棄は行わない。
    /// このStateが持つのは雇用チェックのトグル・アニマ加減算・確定/キャンセル時の遷移のみ。
    /// </summary>
    public sealed class RecruitEmployState : RecruitGridStateBase
    {
        protected override string SelectGuideText => "SELECT\nUNIT";

        private bool _isExistEmployedCharacter  = false;
        private List<CharacterCandidate> _employmentCandidates = null;

        public override void Init( object context )
        {
            base.Init( context );

            // 雇用可能キャラクター一覧はRecruitTopMenuStateが保持している同一インスタンスを受け取る
            // (RecruitScene起動時に一度だけ決定され、以後再抽選されない)
            ReceiveContext( ref _employmentCandidates, context );
            NullCheck.AssertNotNull( _employmentCandidates, nameof( _employmentCandidates ) );

            SetupGrid( LocKey.UI_CMD_EMPLOY, new[]
            {
                "EMPLOY\nCONTARCT",     // 雇用契約
                "CANCEL\nCONTRACT",     // 契約中止
            } );

            // 雇用候補キャラクターは生成・配置済み(CharacterCandidate.Init参照)のため、
            // TroopGridController側で新規生成・再配置はせず、そのままグリッドに表示する
            _gridController.ShowExisting( _employmentCandidates.Select( c => c.Character ).ToList() );

            for( int i = 0; i < _employmentCandidates.Count; ++i )
            {
                var player = _employmentCandidates[i].Character as Player;
                NullCheck.AssertNotNull( player, nameof( player ) );

                _troopEditPresenter.SetCost( i, player.RecruitLogic.Cost );
                _troopEditPresenter.SetChecked( i, player.RecruitLogic.IsEmployed );
            }

            // 前回訪問時の雇用チェック状態を引き継いで反映
            _isExistEmployedCharacter = IsExistEmployedCharacter();

            // 初の雇用フェーズの開始をチュートリアルへ通知
            TutorialFacade.Notify( TriggerType.FirstRecruit );

            // 雇用可能な候補が一人も居ない場合のみ、店主の会話クッション画面を経由する
            ShowNoneAvailableCushionIfNeeded( _employmentCandidates.Count == 0, LocKey.UI_TALK_EMPLOY_NONE_AVAILABLE );
        }

        protected override bool CanAcceptConfirm()
        {
            if( _isConfirmingSelection ) { return false; }

            // 雇用確定によって候補が0体になった場合は選択操作自体を受け付けない
            if( _employmentCandidates.Count == 0 ) { return false; }

            var player = _employmentCandidates[_gridController.SelectedIndex].Character as Player;
            NullCheck.AssertNotNull( player, nameof( player ) );

            // 既に雇用チェックされている場合は雇用前の状態に戻すことができる
            if( player.RecruitLogic.IsEmployed ) { return true; }

            // 所持アニマが足りているかチェック
            if( player.RecruitLogic.Cost <= _userDomain.Anima ) { return true; }

            return false;
        }

        protected override bool CanAcceptOptional()
        {
            return _isExistEmployedCharacter && !_isConfirmingSelection;   // 雇用候補キャラクターが一人もいない場合は完了できない
        }

        /// <summary>
        /// 選択中キャラクターが雇用チェック済みかどうかを返します(CONFIRMアイコン文言の切り替えに使用)。
        /// </summary>
        protected override bool IsSelectedToggled()
        {
            var player = _employmentCandidates[_gridController.SelectedIndex].Character as Player;
            NullCheck.AssertNotNull( player, nameof( player ) );

            return player.RecruitLogic.IsEmployed;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            int index = _gridController.SelectedIndex;
            var player = _employmentCandidates[index].Character as Player;
            NullCheck.AssertNotNull( player, nameof( player ) );

            // 既に雇用チェックされている場合は所持アニマとユニットを雇用前の状態に戻す
            if( player.RecruitLogic.IsEmployed )
            {
                player.RecruitLogic.SetEmployed( false );
                _userDomain.AddAnima( player.RecruitLogic.Cost );
            }
            else
            {
                // 所持アニマチェック
                if( _userDomain.Anima < player.RecruitLogic.Cost )　{　return false;　}

                // 所持アニマを減算して雇用確定
                _userDomain.AddAnima( - player.RecruitLogic.Cost );
                player.RecruitLogic.SetEmployed( true );
            }

            // ユニットの表示を更新
            _troopEditPresenter.SetChecked( index, player.RecruitLogic.IsEmployed );
            // 雇用キャラクターの存在フラグを更新
            _isExistEmployedCharacter = IsExistEmployedCharacter();

            return true;
        }

        /// <summary>
        /// 雇用を行わず、雇用/解雇選択画面(クッション画面)へ戻ります
        /// </summary>
        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            // 未確定の雇用チェックを全て取り消し、消費予定だったアニマを払い戻す
            CancelPendingEmployment();

            Back();

            return true;
        }

        /// <summary>
        /// 雇用完了確認画面へ渡す、雇用チェック済み人数を返します。
        /// </summary>
        protected override int GetToggledCount()
        {
            return _employmentCandidates.Count( c => ( c.Character as Player ).RecruitLogic.IsEmployed );
        }

        /// <summary>
        /// 雇用チェック済み候補の、現在の表示上のインデックス一覧を返します。
        /// </summary>
        protected override List<int> GetCheckedIndices()
        {
            var result = new List<int>();
            for( int i = 0; i < _employmentCandidates.Count; ++i )
            {
                if( ( _employmentCandidates[i].Character as Player ).RecruitLogic.IsEmployed ) { result.Add( i ); }
            }

            return result;
        }

        /// <summary>
        /// 雇用チェック済みキャラクターの契約コスト合計を、所持アニマの減少分として返します
        /// (雇用チェック時点で既に_userDomain.Animaから減算済みのため、その内訳を示す値です)。
        /// </summary>
        protected override int GetPendingAnimaDiff()
        {
            return -_employmentCandidates
                .Where( c => ( c.Character as Player ).RecruitLogic.IsEmployed )
                .Sum( c => ( c.Character as Player ).RecruitLogic.Cost );
        }

        /// <summary>
        /// まだ雇用を確定していないキャラクターの雇用チェックを全て取り消し、
        /// 消費予定だった所持アニマを払い戻します
        /// </summary>
        private void CancelPendingEmployment()
        {
            foreach( var candidate in _employmentCandidates )
            {
                var player = candidate.Character as Player;
                if( !player.RecruitLogic.IsEmployed ) { continue; }

                _userDomain.AddAnima( player.RecruitLogic.Cost );
                player.RecruitLogic.SetEmployed( false );
            }
        }

        /// <summary>
        /// 雇用完了確認ステートでYesが選択された際に呼ばれます。雇用チェック済みの候補を
        /// 自軍へ加入させ、候補一覧から取り除きます。RecruitSceneは終了せず、残りの候補
        /// (未チェックのもの)で引き続き雇用/解雇の選択を続けられます。表示への反映(絞り込み解除
        /// アニメーション)は、この直後にRestartState()から呼ばれるTroopGridController側に委ねます。
        /// </summary>
        public void CommitEmployment()
        {
            // _employmentCandidatesはRecruitTopMenuStateが所有するインスタンスをReceiveContext(参照渡し)で
            // 受け取っているため、新しいリストに差し替えるのではなくこのリスト自体を操作する
            // (差し替えるとRecruitTopMenuState側の一覧に反映されず、次回「雇用」再訪問時に
            // 破棄済みキャラクターへ再アクセスしてしまう)
            for( int i = _employmentCandidates.Count - 1; i >= 0; --i )
            {
                var player = _employmentCandidates[i].Character as Player;
                NullCheck.AssertNotNull( player, nameof( player ) );

                if( !player.RecruitLogic.IsEmployed ) { continue; }

                // 自軍へ加入させた上で、表示用のキャラクターは不要になるため破棄する
                _userDomain.RecruitMember( player.GetStatusRef );
                player.Dispose();

                _employmentCandidates.RemoveAt( i );
            }

            // 生き残ったセルのコスト・チェック表示は、チェック時点で既に正しく設定済みのため
            // 再設定は不要(セル自体はAnimateRestoreDisplay()まで破棄・再生成されない)
            _gridController.SyncSpawnedCharacters( _employmentCandidates.Select( c => c.Character ).ToList() );

            _isExistEmployedCharacter = IsExistEmployedCharacter();
        }

        private bool IsExistEmployedCharacter()
        {
            foreach( var candidate in _employmentCandidates )
            {
                var player = candidate.Character as Player;
                NullCheck.AssertNotNull( player, nameof( player ) );
                if( player.RecruitLogic.IsEmployed ) { return true; }
            }

            return false;
        }
    }
}
