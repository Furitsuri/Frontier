using Frontier.Entities;
using Frontier.StateMachine;
using Frontier.UI;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using Zenject;
using static Constants;
using static DeploymentPhasePresenter;

namespace Frontier.FormTroop
{
    public class RecruitPhasePresenter : PhasePresenterBase
    {
        [Inject] private UserDomain _userDomain = null;

        private bool _isSlideAnimationPlaying = false;
        private SlideDirection _slideDirection;
        private RecruitUISystem _recruitmentUI                                  = null;
        private ReadOnlyCollection<CharacterCandidate> _refEmploymentCandidates = null;
        private CharacterCandidate[] _focusEmployments                          = new CharacterCandidate[EMPLOYMENT_SHOWABLE_CHARACTERS_NUM];
        private Action<SlideDirection> _onCompletedeSlideAnimation;

        public void Init()
        {
            _recruitmentUI = _uiSystem.RecruitUi;
            _recruitmentUI.Init();
        }

        public void Update()
        {
            _recruitmentUI.SetMoneyValue( _userDomain.Money );  // 所持金の更新
            UpdateSlideAnimation();
        }

        public void Exit()
        {
            _recruitmentUI.Exit();
        }

        /// <summary>
        /// 配置候補キャラクターリストから、指定されたインデックスを中心にキャラクターを抽出して表示させます
        /// </summary>
        /// <param name="focusCharaIndex"></param>
        public void SetFocusCharacters( int focusCharaIndex )
        {
            int arrayLength = _focusEmployments.Length;  // 奇数前提であることに注意
            int centralIndex = arrayLength / 2;
            int candidateCount = _refEmploymentCandidates.Count;

            if( candidateCount <= centralIndex )
            {
                candidateCount = arrayLength;
            }

            // 中央のインデックスを基準に、左右に配置するキャラクターを決定していく
            // 配列の端を超えた場合はループさせる
            for( int i = 0; i < _focusEmployments.Length; ++i )
            {
                // 「中央」を中心に左右に割り当てる（不足分はループする）
                int offset = ( i - centralIndex + candidateCount ) % candidateCount;
                int targetIndex = ( focusCharaIndex + offset ) % candidateCount;

                if( targetIndex.IsBetween( 0, _refEmploymentCandidates.Count - 1 ) )
                {
                    _focusEmployments[i] = _refEmploymentCandidates[targetIndex];
                }
                else { _focusEmployments[i] = null; }
            }

            _recruitmentUI.EmploymentSelectUI.AssignSelectCandidates( ref _focusEmployments );

            RefreshFocusEmploymentCharacter();
        }

        public void ResetEmploymentCharacterDispPosition()
        {
            _recruitmentUI.EmploymentSelectUI.ResetDeploymentCharacterDispPositions();
        }

        /// <summary>
        /// フォーカス中の配置可能キャラクター情報を更新します
        /// </summary>
        public void RefreshFocusEmploymentCharacter()
        {
            // フォーカス中のキャラクターのパラメータの表示
            Debug.Assert( _focusEmployments.Length % 2 == 1 );  // 奇数であることが前提
            _recruitmentUI.EmploymentSelectUI.FocusCharaParamUI.AssignCharacter( _focusEmployments[_focusEmployments.Length / 2].Character, LAYER_MASK_INDEX_DEPLOYMENT_FOCUS );
        }

        public void RefreshCentralCandidateEmployed()
        {
            int centralIndex = _focusEmployments.Length / 2;
            var player = _focusEmployments[ centralIndex ].Character as Player;
            NullCheck.AssertNotNull( player, nameof( player ) );

            // フォーカス中のキャラクター表示の更新
            _recruitmentUI.EmploymentSelectUI.RefreshCandidate( centralIndex, ref _focusEmployments[ centralIndex ] );
        }

        public void ClearFocusCharacter()
        {
            _recruitmentUI.EmploymentSelectUI.ClearSelectCharacter();
        }

        public void AssignEmploymentCandidates( ReadOnlyCollection<CharacterCandidate> candidates )
        {
            _refEmploymentCandidates = candidates;
        }

        /// <summary>
        /// キャラクター選択UIの表示・非表示を切り替えます
        /// </summary>
        /// <param name="isActive"></param>
        public void SetActiveCharacterSelectUIs( bool isActive )
        {
            _recruitmentUI.EmploymentSelectUI.SetActive( isActive );
        }

        public void SlideAnimationCharacterSelectionDisplay( SlideDirection direction, Action<SlideDirection> onCompleted )
        {
            _isSlideAnimationPlaying = true;
            _slideDirection = direction;
            _onCompletedeSlideAnimation = onCompleted;

            _recruitmentUI.EmploymentSelectUI.StartSlideAnimation( direction );
        }

        /// <summary>
        /// キャラクター選択UIのスライドアニメーションの更新
        /// </summary>
        private void UpdateSlideAnimation()
        {
            if( _isSlideAnimationPlaying )
            {
                _isSlideAnimationPlaying = !_recruitmentUI.EmploymentSelectUI.UpdateSlideAnimation();
                if( !_isSlideAnimationPlaying )
                {
                    _onCompletedeSlideAnimation?.Invoke( _slideDirection );
                }
            }
        }
    }
}