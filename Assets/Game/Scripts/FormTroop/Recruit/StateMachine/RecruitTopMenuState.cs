using Frontier.Entities;
using Frontier.StateMachine;
using Frontier.UI;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Zenject;
using static Constants;
using static Frontier.Loaders.BattleFileLoader;

namespace Frontier.FormTroop
{
    /// <summary>
    /// 雇用フェーズ開始時に表示する「雇用/解雇」選択のクッションステート。
    /// 雇用可能キャラクター一覧はこのステートがRecruitScene起動時に一度だけ決定・保持し、
    /// 以後(「雇用」→キャンセルで本ステートに戻ってきても)再抽選しない。
    /// 「雇用」選択時は既存の雇用候補選択画面(RecruitRootState)へ、「解雇」選択時は
    /// 自軍メンバー一覧画面(RecruitDismissState)へ遷移する。
    /// </summary>
    public sealed class RecruitTopMenuState : RecruitPhaseStateBase
    {
        private enum RecruitTopMenuTransitTag
        {
            EMPLOY = 0,
            DISMISS,
            CONFIRM_CANCEL,
        }

        [Inject] private UserDomain _userDomain                     = null;
        [Inject] private CharacterFactory _characterFactory         = null;
        [Inject] private TalkWindowPresenter _talkWindowPresenter   = null;

        private bool _isCancelled = false;  // 雇用を行わずにRecruitScene自体を終了するか
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;
        private List<CharacterCandidate> _employmentCandidates = new List<CharacterCandidate>();
        private UnitLevelStatsContainer _unitLevelStatsContainer = null;

        public override void Init( object context )
        {
            base.Init( context );

            _isCancelled = false;

            // 雇用可能キャラクター一覧はRecruitScene起動時に一度だけ決定する
            string json = File.ReadAllText( "Assets/Resources/CharactersData/UnitLevelStats/UnitLevelStatsData.json" );
            _unitLevelStatsContainer = JsonUtility.FromJson<UnitLevelStatsContainer>( json );
            SetupEmploymentCandidates();

            List<int> commandIndices = new List<int>( ( int ) RECRUIT_TOP_MENU_OPTION_TAG.NUM );
            for( int i = 0; i < ( int ) RECRUIT_TOP_MENU_OPTION_TAG.NUM; ++i )
            {
                commandIndices.Add( i );
            }

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref commandIndices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _presenter.SetActiveTopMenu( true );
            _presenter.SetTopMenuSelectedIndex( _cmdIdxVal.value );

            _talkWindowPresenter.Show( "shopkeeper_greeting" );
        }

        public override object ExitState()
        {
            _presenter.SetActiveTopMenu( false );
            _talkWindowPresenter.Hide();

            if( _isCancelled )
            {
                // キャンセル時は雇用予約を全て取り消し、消費した所持アニマを払い戻す
                CancelAllEmployment();
            }
            else
            {
                JoinCandidates();
            }

            RemoveEmploymentCandidates();

            return base.ExitState();
        }

        /// <summary>
        /// 雇用候補選択画面(RecruitRootState)からキャンセルで戻ってきた際、メニュー表示を復帰します
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();

            _presenter.SetActiveTopMenu( true );
            _presenter.SetTopMenuSelectedIndex( _cmdIdxVal.value );

            _talkWindowPresenter.Show( "shopkeeper_greeting" );
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.VERTICAL_CURSOR, "SELECT",             CanAcceptDefault, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,         "CONFIRM",            CanAcceptDefault, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL,          "CANCEL\nRECRUIT",    CanAcceptDefault, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        protected override bool AcceptDirection( InputContext context )
        {
            if( !_commandList.OperateListCursor( context.Cursor ) ) { return false; }

            _presenter.SetTopMenuSelectedIndex( _cmdIdxVal.value );

            return true;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            switch( ( RECRUIT_TOP_MENU_OPTION_TAG ) _cmdIdxVal.value )
            {
                case RECRUIT_TOP_MENU_OPTION_TAG.EMPLOY:
                    // 画面遷移のため即座に隠す(子への遷移はPauseState止まりでExitStateが呼ばれないため)
                    _presenter.SetActiveTopMenu( false );
                    _talkWindowPresenter.Hide();
                    // 雇用可能キャラクター一覧(このステートが保持し続ける同一インスタンス)を渡す
                    SetSendTransitionContext( _employmentCandidates );
                    TransitState( ( int ) RecruitTopMenuTransitTag.EMPLOY );
                    break;

                case RECRUIT_TOP_MENU_OPTION_TAG.DISMISS:
                    // 画面遷移のため即座に隠す(子への遷移はPauseState止まりでExitStateが呼ばれないため)
                    _presenter.SetActiveTopMenu( false );
                    _talkWindowPresenter.Hide();
                    TransitState( ( int ) RecruitTopMenuTransitTag.DISMISS );
                    break;
            }

            return true;
        }

        /// <summary>
        /// 雇用を行わずにRecruitルーチンから脱出してよいかの確認ステートへ遷移します
        /// </summary>
        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            TransitState( ( int ) RecruitTopMenuTransitTag.CONFIRM_CANCEL );

            return true;
        }

        /// <summary>
        /// 雇用を行わずにRecruitルーチンから脱出することを要求します
        /// </summary>
        public void RequestCancelExit()
        {
            _isCancelled = true;
        }

        /// <summary>
        /// 雇用可能キャラクター一覧を生成します(RecruitScene起動時に一度だけ呼ばれる)
        /// </summary>
        private void SetupEmploymentCandidates()
        {
            _employmentCandidates.Clear();

            for( int i = 0; i < EMPLOYABLE_CHARACTERS_NUM; ++i )
            {
                Player player = CreateEmploymentCandidate( _userDomain.StageLevel, i );

                // 配置候補キャラクターを生成・初期化してスナップショットと共にリストに追加
                CharacterCandidate candidate = _hierarchyBld.InstantiateWithDiContainer<CharacterCandidate>( false );
                candidate.Init( player, null );

                _employmentCandidates.Add( candidate );
            }
        }

        /// <summary>
        /// 雇用チェックされたキャラクターをキャラクター辞書に登録します
        /// </summary>
        private void JoinCandidates()
        {
            foreach( var candidate in _employmentCandidates )
            {
                var player = candidate.Character as Player;
                if( !player.RecruitLogic.IsEmployed ) { continue; }
                _userDomain.RecruitMember( player.GetStatusRef );
            }
        }

        /// <summary>
        /// 雇用予約を全て取り消し、消費した所持アニマを払い戻します
        /// </summary>
        private void CancelAllEmployment()
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
        /// 不要な雇用候補キャラクターを破棄します
        /// </summary>
        private void RemoveEmploymentCandidates()
        {
            for( int i = 0; i < _employmentCandidates.Count; ++i )
            {
                Player player = _employmentCandidates[i].Character as Player;
                player.RestoreMaterialsOriginalColor();

                if( player.RecruitLogic.IsEmployed )
                {
                    player.OnRecruitExit();
                    continue;
                }

                player.Dispose();
            }
        }

        /// <summary>
        /// 雇用候補キャラクターを生成します
        /// </summary>
        /// <returns></returns>
        private Player CreateEmploymentCandidate( int level, int characterIndex )
        {
            ( int unitTypeIndex, int cost, CharacterDeployData deployData ) =
                RecruitFormula.GenerateEmploymentCandidateData( level, characterIndex, _unitLevelStatsContainer, _employmentCandidates );

            Player player = _characterFactory.CreateCharacter( CHARACTER_TAG.PLAYER, unitTypeIndex, deployData ) as Player;
            player.OnRecruitEnter( cost );

            return player;
        }
    }
}
