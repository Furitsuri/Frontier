using Frontier.Entities;
using Frontier.Tutorial;
using System.Collections.Generic;
using Zenject;
using static Constants;
using static Frontier.BattleFileLoader;

namespace Frontier.FormTroop
{
    public sealed class RecruitRootState : RecruitPhaseStateBase
    {
        private enum RecruitRootTransitTag
        {
            CHARACTER_STATUS = 0,
            CONFIRM,
        }

        [Inject] private UserDomain _userDomain                     = null;
        [Inject] private CharacterFactory _characterFactory         = null;

        private bool _isExistEmployedCharacter  = false;
        private int _focusCharacterIndex        = 0;     // フォーカス中のキャラクターインデックス
        private string[] _inputConfirmStrings;
        private List<CharacterCandidate> _employmentCandidates = new List<CharacterCandidate>();
        private InputCodeStringWrapper _inputConfirmStrWrapper = null;

        public override void Init()
        {
            base.Init();
            
            _isExistEmployedCharacter   = false;
            _focusCharacterIndex        = 0;

            // CONFIRMアイコンの文字列を設定
            _inputConfirmStrings = new string[]
            {
                "EMPLOY\nCONTARCT",     // 雇用契約
                "CANCEL\nCONTRACT",     // 契約中止
            };

            _inputConfirmStrWrapper = new InputCodeStringWrapper( _inputConfirmStrings[0] );

            SetupEmploymentCandidates();
            _presenter.SetActiveCharacterSelectUIs( true );
            _presenter.AssignEmploymentCandidates( _employmentCandidates.AsReadOnly() );
            _presenter.SetFocusCharacters( _focusCharacterIndex );

            // 初の雇用フェーズの開始をチュートリアルへ通知
            TutorialFacade.Notify( TriggerType.FirstRecruit );
        }

        public override bool Update()
        {
            // 基底の更新は行わない
            // if( base.Update() ) { return true; }

            _inputConfirmStrWrapper.Explanation = 
                _employmentCandidates[_focusCharacterIndex].Character is Player player && player.RecruitLogic.IsEmployed ?
                _inputConfirmStrings[1] : _inputConfirmStrings[0];

            return ( 0 <= TransitIndex );
        }

        public override void ExitState()
        {
            JoinCandidates();
            RemoveEmploymentCandidates();

            base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,   "SELECT\nUNIT",             CanAcceptDefault,   new AcceptDirectionInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,      _inputConfirmStrWrapper,    CanAcceptConfirm,   new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.INFO,         "STATUS",                   CanAcceptDefault,   new AcceptBooleanInput( AcceptInfo ), 0.0f, hashCode),
               (GuideIcon.OPT2,         "COMPLETE",                 CanAcceptOptional,  new AcceptBooleanInput( AcceptOptional ), 0.0f, hashCode)
            );
        }

        protected override bool CanAcceptConfirm()
        {
            var player = _employmentCandidates[_focusCharacterIndex].Character as Player;
            NullCheck.AssertNotNull( player, nameof( player ) );

            // 既に雇用チェックされている場合は雇用前の状態に戻すことができる
            if( player.RecruitLogic.IsEmployed ) { return true; }

            // 所持金が足りているかチェック
            if( player.RecruitLogic.Cost <= _userDomain.Money ) { return true; }

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
        protected override bool AcceptDirection( Direction dir )
        {
            bool isOperated = false;

            switch( dir )
            {
                case Direction.LEFT:
                    {
                        _presenter.SlideAnimationCharacterSelectionDisplay( SlideDirection.LEFT, OnCompleteSlideAnimation );
                        isOperated = true;
                    }
                    break;
                case Direction.RIGHT:
                    {
                        _presenter.SlideAnimationCharacterSelectionDisplay( SlideDirection.RIGHT, OnCompleteSlideAnimation );
                        isOperated = true;
                    }
                    break;
            }

            return isOperated;
        }

        protected override bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) { return false; }

            var player = _employmentCandidates[_focusCharacterIndex].Character as Player;
            NullCheck.AssertNotNull( player, nameof( player ) );

            // 既に雇用チェックされている場合は所持金とユニットを雇用前の状態に戻す
            if( player.RecruitLogic.IsEmployed )
            {
                player.RecruitLogic.SetEmployed( false );
                _userDomain.AddMoney( player.RecruitLogic.Cost );
            }
            else
            {
                // 所持金チェック
                if( _userDomain.Money < player.RecruitLogic.Cost )　{　return false;　}

                // 所持金を減算して雇用確定
                _userDomain.AddMoney( - player.RecruitLogic.Cost );
                player.RecruitLogic.SetEmployed( true );
            }

            // ユニットの表示を更新
            _presenter.RefreshCentralCandidateEmployed();
            // 雇用キャラクターの存在フラグを更新
            _isExistEmployedCharacter = IsExistEmployedCharacter();

            return true;
        }

        protected override bool AcceptInfo( bool isInput )
        {
            if( !isInput ) { return false; }

            // ステータス表示ステートに対象キャラクターを渡す
            Handler.ReceiveContext( _employmentCandidates[_focusCharacterIndex].Character );
            // キャラクターステータス表示ステートへ遷移
            TransitState( ( int ) RecruitRootTransitTag.CHARACTER_STATUS );

            return true;
        }

        protected override bool AcceptOptional( bool isInput )
        {
            if( !isInput ) { return false; }

            // 雇用完了確認ステートへ遷移
            TransitState( ( int ) RecruitRootTransitTag.CONFIRM );

            return true;
        }

        private void SetupEmploymentCandidates()
        {
            _employmentCandidates.Clear();

            for( int i = 0; i < EMPLOYABLE_CHARACTERS_NUM; ++i )
            {
                Player player = CreateEmploymentCandidate( i );

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
                _userDomain.RecruitMember( player );
            }
        }

        /// <summary>
        /// 不要な雇用候補キャラクターを破棄します
        /// </summary>
        private void RemoveEmploymentCandidates()
        {
            for( int i = 0; i < _employmentCandidates.Count; ++i )
            // foreach( var unit in _employmentCandidates )
            {
                Player player = _employmentCandidates[i].Character as Player;
                player.RestoreMaterialsOriginalColor();

                if( player.RecruitLogic.IsEmployed ) { continue; }

                player.Dispose();
            }
        }

        /// <summary>
        /// スライドアニメーション完了時のコールバック
        /// </summary>
        /// <param name="direction"></param>
        private void OnCompleteSlideAnimation( SlideDirection direction )
        {
            _presenter.ClearFocusCharacter();

            if( direction == SlideDirection.LEFT )
            {
                _focusCharacterIndex = ( ( _focusCharacterIndex - 1 ) + _employmentCandidates.Count ) % _employmentCandidates.Count;
            }
            else
            {
                _focusCharacterIndex = ( _focusCharacterIndex + 1 ) % _employmentCandidates.Count;
            }

            _presenter.SetFocusCharacters( _focusCharacterIndex );
            _presenter.ResetEmploymentCharacterDispPosition();
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

        /// <summary>
        /// 雇用候補キャラクターを生成します
        /// </summary>
        /// <returns></returns>
        private Player CreateEmploymentCandidate( int characterIndex )
        {
            ( int unitTypeIndex, int cost, CharacterStatusData statusData ) =
                RecruitFormula.GenerateEmploymentCandidateData( _userDomain.StageLevel, characterIndex, _employmentCandidates );

            Player player = _characterFactory.CreateCharacter( CHARACTER_TAG.PLAYER, unitTypeIndex, statusData ) as Player;
            player.OnRecruitEnter( cost );

            return player;
        }
    }
}