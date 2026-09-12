using Frontier.Entities;
using Frontier.StateMachine;
using Frontier.Tutorial;
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
    /// 「雇用」選択時に表示する雇用候補一覧グリッド画面。
    /// グリッド表示・カーソル移動・キャラクターのライフサイクルは、解雇画面(RecruitDismissState)
    /// と共通のTroopGridControllerに委譲する。雇用候補キャラクター(CharacterCandidate)は
    /// RecruitTopMenuStateが生成・所有・破棄するため、このStateはShowExisting()で表示を
    /// 借りるだけで、破棄は行わない。このStateが持つのは雇用チェックのトグル・アニマ加減算・
    /// 確定/キャンセル時の遷移のみ。
    /// </summary>
    public sealed class RecruitRootState : RecruitPhaseStateBase
    {
        private enum RecruitRootTransitTag
        {
            CHARACTER_STATUS = 0,
            CONFIRM,
            GREETING,
        }

        [Inject] private UserDomain _userDomain = null;
        [Inject] private GeneralHeaderPresenter _headerPresenter = null;

        private TroopEditPresenter _troopEditPresenter      = null;
        private CharacterParameterPresenter _paramPresenter = null;
        private TroopGridController _gridController         = null;

        private bool _isExistEmployedCharacter  = false;
        private string[] _inputConfirmStrings;
        private List<CharacterCandidate> _employmentCandidates = null;
        private InputCodeStringWrapper _inputConfirmStrWrapper = null;

        public override void Init( object context )
        {
            base.Init( context );

            // CONFIRMアイコンの文字列を設定
            _inputConfirmStrings = new string[]
            {
                "EMPLOY\nCONTARCT",     // 雇用契約
                "CANCEL\nCONTRACT",     // 契約中止
            };

            _inputConfirmStrWrapper = new InputCodeStringWrapper( _inputConfirmStrings[0] );

            // 雇用可能キャラクター一覧はRecruitTopMenuStateが保持している同一インスタンスを受け取る
            // (RecruitScene起動時に一度だけ決定され、以後再抽選されない)
            ReceiveContext( ref _employmentCandidates, context );
            NullCheck.AssertNotNull( _employmentCandidates, nameof( _employmentCandidates ) );

            LazyInject.GetOrCreate( ref _troopEditPresenter, () => _hierarchyBld.InstantiateWithDiContainer<TroopEditPresenter>( false ) );
            _troopEditPresenter.Init();
            _headerPresenter.SetStateTitle( LocKey.UI_CMD_EMPLOY );

            LazyInject.GetOrCreate( ref _paramPresenter, () => _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>(
                new object[] { _troopEditPresenter.CharacterParamUI, false }, false ) );
            _paramPresenter.Init();

            LazyInject.GetOrCreate( ref _gridController, () => _hierarchyBld.InstantiateWithDiContainer<TroopGridController>( false ) );
            _gridController.Init( _troopEditPresenter, _paramPresenter );

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
            // (会話を閉じるとRestartState()でこの画面の表示に戻る)。クッション画面表示中は
            // カーソル・パラメータパネルを隠し、表示するメッセージはcontext経由で渡す。
            if( _employmentCandidates.Count == 0 )
            {
                _gridController.HideSelectionDisplay();
                SetSendTransitionContext( new TalkWindowCushionContext( LocKey.UI_TALK_SHOPKEEPER_NAME, LocKey.UI_TALK_EMPLOY_NONE_AVAILABLE ) );
                TransitState( ( int ) RecruitRootTransitTag.GREETING );
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
            // 基底の更新は行わない
            // if( base.Update() ) { return true; }

            // 雇用確定によって候補が0体になった場合はCONFIRMアイコンの文字列更新をスキップする
            if( _employmentCandidates.Count > 0 )
            {
                var player = _employmentCandidates[_gridController.SelectedIndex].Character as Player;
                NullCheck.AssertNotNull( player, nameof( player ) );

                _inputConfirmStrWrapper.Explanation = player.RecruitLogic.IsEmployed ? _inputConfirmStrings[1] : _inputConfirmStrings[0];
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
               (GuideIcon.ALL_CURSOR,           "SELECT\nUNIT",             CanAcceptDefault,       new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,              _inputConfirmStrWrapper,    CanAcceptConfirm,       new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL,               "BACK",                     CanAcceptDefault,       new AcceptContextInput( AcceptCancel ), 0.0f, hashCode),
               (GuideIcon.INFO,                 "STATUS",                   CanAcceptInfo,          new AcceptContextInput( AcceptInfo ), 0.0f, hashCode),
               (GuideIcon.OPT2,                 "COMPLETE",                 CanAcceptOptional,      new AcceptContextInput( AcceptOpt2 ), 0.0f, hashCode)
            );
        }

        protected override bool CanAcceptConfirm()
        {
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
            return _isExistEmployedCharacter;   // 雇用候補キャラクターが一人もいない場合は完了できない
        }

        /// <summary>
        /// 雇用確定によって候補が0体になった場合はステータス表示への遷移を受け付けない
        /// </summary>
        protected override bool CanAcceptInfo()
        {
            return _employmentCandidates.Count > 0;
        }

        /// <summary>
        /// 方向入力を受け取り、選択グリッドを操作します
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptDirection( InputContext context )
        {
            return _gridController.MoveSelection( context.Cursor );
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
        /// 自軍へ加入させ、表示(候補一覧・グリッド)から取り除きます。RecruitSceneは終了せず、
        /// 残りの候補(未チェックのもの)で引き続き雇用/解雇の選択を続けられます。
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

            _gridController.ShowExisting( _employmentCandidates.Select( c => c.Character ).ToList() );

            for( int i = 0; i < _employmentCandidates.Count; ++i )
            {
                var player = _employmentCandidates[i].Character as Player;
                _troopEditPresenter.SetCost( i, player.RecruitLogic.Cost );
                _troopEditPresenter.SetChecked( i, player.RecruitLogic.IsEmployed );
            }

            _isExistEmployedCharacter = IsExistEmployedCharacter();
        }

        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            // ステータス表示ステートに対象キャラクターを渡す
            SetSendTransitionContext( _employmentCandidates[_gridController.SelectedIndex].Character );
            // キャラクターステータス表示ステートへ遷移
            TransitState( ( int ) RecruitRootTransitTag.CHARACTER_STATUS );

            return true;
        }

        protected override bool AcceptOpt2( InputContext context )
        {
            if( !base.AcceptOpt2( context ) ) { return false; }

            // 確認画面の会話文言(単数/複数)を選ぶための、雇用チェック済み人数を渡す
            int employedCount = _employmentCandidates.Count( c => ( c.Character as Player ).RecruitLogic.IsEmployed );
            SetSendTransitionContext( employedCount );

            // 雇用完了確認ステートへ遷移
            TransitState( ( int ) RecruitRootTransitTag.CONFIRM );

            return true;
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
