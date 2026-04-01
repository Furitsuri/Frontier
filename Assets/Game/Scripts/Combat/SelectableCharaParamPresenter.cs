using Frontier.Entities;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    public class SelectableCharaParamPresenter : CharacterSelectionPresenter, IConfirmPresenter
    {
        private SelectableCharaParamUI _recruitmentUI = null;

        [Inject]
        public SelectableCharaParamPresenter( IUiSystem uiSystem, HierarchyBuilderBase hierarchyBld ) : base( uiSystem.BattleUi.SelectableCharaParam, RECRUIT_SHOWABLE_CHARACTERS_NUM, false, hierarchyBld )
        {
            _uiSystem       = uiSystem;
            _recruitmentUI  = _uiSystem.BattleUi.SelectableCharaParam;
        }

        public override void Init()
        {
            base.Init();

            _recruitmentUI.Init();
        }

        public void Update()
        {
            UpdateSlideAnimation();
        }

        public override void Exit()
        {
            base.Exit();
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
            var player = _focusCandidates[centralIndex].Character as Player;
            NullCheck.AssertNotNull( player, nameof( player ) );
        }

        public void SetActiveConfirmUI( bool isActive )
        {
        }

        public void ApplyColor2Options( int selectIndex )
        {
        }
    }
}