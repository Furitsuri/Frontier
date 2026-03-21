using Frontier.Entities;
using Frontier.StateMachine;
using Frontier.UI;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.FormTroop
{
    public class RecruitPhasePresenter : CharacterSelectionPresenter, IConfirmPresenter
    {
        [Inject] private UserDomain _userDomain = null;

        private RecruitUISystem _recruitmentUI = null;

        [Inject]
        public RecruitPhasePresenter( IUiSystem uiSystem, HierarchyBuilderBase hierarchyBld ) : base( uiSystem.RecruitUi.EmploymentSelectUI, RECRUIT_SHOWABLE_CHARACTERS_NUM, false, hierarchyBld )
        {
            _uiSystem      = uiSystem;
            _recruitmentUI = _uiSystem.RecruitUi;
        }

        public void Init()
        {
            base.Init( _uiSystem.RecruitUi.EmploymentSelectUI, RECRUIT_SHOWABLE_CHARACTERS_NUM, false );

            _recruitmentUI.Init();
        }

        public void Update()
        {
            _recruitmentUI.SetMoneyValue( _userDomain.Money );  // 所持金の更新
            UpdateSlideAnimation();
        }

        public override void Exit()
        {
            base.Exit();

            _recruitmentUI.Exit();
        }

        public override void SetFocusCharacters( int focusCharaIndex )
        {
            base.SetFocusCharacters( focusCharaIndex );

            RefreshFocusCharacterParameter();
        }

        /// <summary>
        /// フォーカス中の配置可能キャラクター情報を更新します
        /// </summary>
        public void RefreshFocusCharacterParameter()
        {
            // フォーカス中のキャラクターのパラメータの表示
            Debug.Assert( _focusCandidates.Length % 2 == 1 );  // 奇数であることが前提
            _parameterPresenter.AssignCharacter( _focusCandidates[_focusCandidates.Length / 2].Character, LAYER_MASK_INDEX_DEPLOYMENT_FOCUS );
        }

        /// <summary>
        /// 表示中央のキャラクター(フォーカス中)の雇用チェック状態に応じて表示を更新します
        /// </summary>
        public void RefreshCentralCandidateEmployed()
        {
            int centralIndex = _focusCandidates.Length / 2;
            var player = _focusCandidates[ centralIndex ].Character as Player;
            NullCheck.AssertNotNull( player, nameof( player ) );

            // フォーカス中のキャラクター表示の更新
            _recruitmentUI.EmploymentSelectUI.RefreshCandidate( centralIndex, ref _focusCandidates[ centralIndex ] );
        }

        public void SetActiveConfirmUI( bool isActive )
        {
            _recruitmentUI.ConfirmEmploymentUI.gameObject.SetActive( isActive );
        }

        public void ApplyColor2Options( int selectIndex )
        {
            _recruitmentUI.ConfirmEmploymentUI.ApplyTextColor( selectIndex );
        }
    }
}