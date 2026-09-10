using Frontier.Entities;
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
        }

        [Inject] private UserDomain _userDomain = null;

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
            _troopEditPresenter.SetTitleKey( LocKey.UI_CMD_EMPLOY );

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
                _troopEditPresenter.SetEmployed( i, player.RecruitLogic.IsEmployed );
            }

            // 前回訪問時の雇用チェック状態を引き継いで反映
            _isExistEmployedCharacter = IsExistEmployedCharacter();

            // 初の雇用フェーズの開始をチュートリアルへ通知
            TutorialFacade.Notify( TriggerType.FirstRecruit );
        }

        public override bool Update()
        {
            // 基底の更新は行わない
            // if( base.Update() ) { return true; }

            var player = _employmentCandidates[_gridController.SelectedIndex].Character as Player;
            NullCheck.AssertNotNull( player, nameof( player ) );

            _inputConfirmStrWrapper.Explanation = player.RecruitLogic.IsEmployed ? _inputConfirmStrings[1] : _inputConfirmStrings[0];

            return ( 0 <= TransitIndex );
        }

        public override object ExitState()
        {
            _gridController.Close();

            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,           "SELECT\nUNIT",             CanAcceptDefault,       new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,              _inputConfirmStrWrapper,    CanAcceptConfirm,       new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL,               "BACK",                     CanAcceptDefault,       new AcceptContextInput( AcceptCancel ), 0.0f, hashCode),
               (GuideIcon.INFO,                 "STATUS",                   CanAcceptDefault,       new AcceptContextInput( AcceptInfo ), 0.0f, hashCode),
               (GuideIcon.OPT2,                 "COMPLETE",                 CanAcceptOptional,      new AcceptContextInput( AcceptOpt2 ), 0.0f, hashCode)
            );
        }

        protected override bool CanAcceptConfirm()
        {
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
            _troopEditPresenter.SetEmployed( index, player.RecruitLogic.IsEmployed );
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
