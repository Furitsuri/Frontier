using Frontier.Entities;
using System.Collections.Generic;
using Zenject;
using static Constants;
using static Frontier.BattleFileLoader;

namespace Frontier.FormTroop
{
    public sealed class RecruitRootState : RecruitPhaseStateBase
    {
        [Inject] private UserDomain _userDomain             = null;
        [Inject] private CharacterFactory _characterFactory = null;

        private int _focusCharacterIndex = 0;     // フォーカス中のキャラクターインデックス
        private List<CharacterCandidate> _employmentedCandidates = new List<CharacterCandidate>();

        public override void Init()
        {
            base.Init();

            _focusCharacterIndex = 0;
            SetupEmploymentCandidates();
            _presenter.SetActiveCharacterSelectUIs( true );
            _presenter.AssignEmploymentCandidates( _employmentedCandidates.AsReadOnly() );
            _presenter.SetFocusCharacters( _focusCharacterIndex );
        }

        public override bool Update()
        {
            // 基底の更新は行わない
            // if( base.Update() ) { return true; }

            return ( 0 <= TransitIndex );
        }

        public override void ExitState()
        {
            RemoveEmploymentCandidates();

            base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,   "SELECT UNIT",  CanAcceptDefault, new AcceptDirectionInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,      "RECRUIT UNIT", CanAcceptConfirm, new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.TOOL,         "HIRED UNIT",   CanAcceptTool, new AcceptBooleanInput( AcceptTool ), 0.0f, hashCode),
               (GuideIcon.INFO,         "STATUS",       CanAcceptInfo, new AcceptBooleanInput( AcceptInfo ), 0.0f, hashCode),
               (GuideIcon.OPT2,         "COMPLETE",     CanAcceptOptional, new AcceptBooleanInput( AcceptOptional ), 0.0f, hashCode)
            );
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

        private void SetupEmploymentCandidates()
        {
            for( int i = 0; i < EMPLOYABLE_CHARACTERS_NUM; ++i )
            {
                Player player = CreateEmploymentCandidate( i );

                // 配置候補キャラクターを生成・初期化してスナップショットと共にリストに追加
                CharacterCandidate candidate = _hierarchyBld.InstantiateWithDiContainer<CharacterCandidate>( false );
                candidate.Init( player, null );

                _employmentedCandidates.Add( candidate );
            }
        }

        private void RemoveEmploymentCandidates()
        {
            foreach( var unit in _employmentedCandidates )
            {
                Player player = unit.Character as Player;

                if( player.RecruitLogic.IsEmployed ) { continue; }

                player.Dispose();
                _employmentedCandidates.Remove( unit );
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
                _focusCharacterIndex = ( ( _focusCharacterIndex - 1 ) + _employmentedCandidates.Count ) % _employmentedCandidates.Count;
            }
            else
            {
                _focusCharacterIndex = ( _focusCharacterIndex + 1 ) % _employmentedCandidates.Count;
            }

            _presenter.SetFocusCharacters( _focusCharacterIndex );
            _presenter.ResetEmploymentCharacterDispPosition();
        }

        /// <summary>
        /// 雇用候補キャラクターを生成します
        /// </summary>
        /// <returns></returns>
        private Player CreateEmploymentCandidate( int characterIndex )
        {
            ( int unitTypeIndex, int cost, CharacterStatusData statusData ) =
                RecruitFormula.GenerateEmploymentCandidateData( _userDomain.StageLevel, characterIndex, _employmentedCandidates );

            Player player = _characterFactory.CreateCharacter( CHARACTER_TAG.PLAYER, unitTypeIndex, statusData ) as Player;
            player.OnRecruitEnter( cost );

            return player;
        }
    }
}