using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using System.Collections.Generic;
using Zenject;
using static Constants;
using static Frontier.UI.BattleUISystem;

namespace Frontier.Battle
{
    public class BattleRoutinePresenter : PhasePresenterBase, IConfirmPresenter
    {
        UI.CharacterParameterUI[] _parameterUIs;

        public void Setup()
        {
            _parameterUIs = new UI.CharacterParameterUI[( int )ParameterWindowType.NUM]
            {
                _uiSystem.BattleUi.ParameterView.PlayerParameter,
                _uiSystem.BattleUi.ParameterView.EnemyParameter
            };
        }

        public void Update() { }

        public void SetActiveBattleUI( bool isActive )
        {
            _uiSystem.BattleUi.gameObject.SetActive( isActive );
        }

        public void SetActiveConfirmUI( bool isActive )
        {
            _uiSystem.BattleUi.ConfirmTurnEnd.gameObject.SetActive( isActive );
        }

        public void SetActiveParamView( bool isActive, ParameterWindowType winType )
        {
            _parameterUIs[( int ) winType].gameObject.SetActive( isActive );
        }

        public void ApplyColor2Options( int selectIndex )
        {
            _uiSystem.BattleUi.ConfirmTurnEnd.ApplyTextColor( selectIndex );
        }

        /// <summary>
        /// ステージクリア時のUIとアニメーションを表示します
        /// </summary>
        public void StartStageClearAnim()
        {
            _uiSystem.BattleUi.ToggleStageClearUI( true );
            _uiSystem.BattleUi.StartStageClearAnim();
        }

        /// <summary>
        /// ゲームオーバー時のUIとアニメーションを表示します
        /// </summary>
        public void StartGameOverAnim()
        {
            _uiSystem.BattleUi.ToggleGameOverUI( true );
            _uiSystem.BattleUi.StartGameOverAnim();
        }

        public void AssignCharacterToParameterView( Character character, ParameterWindowType winType )
        {
            int[] layerMaskIndex = new int[]
            {
                LAYER_MASK_INDEX_PLAYER,
                LAYER_MASK_INDEX_ENEMY
            };

            _parameterUIs[(int)winType].AssignCharacter( character, layerMaskIndex[( int)winType] );
        }

        public bool IsActiveStageClearAnimation()
        {
            return _uiSystem.BattleUi.StageClear.isActiveAndEnabled;
        }

        public bool IsActiveGameOverAnimation()
        {
            return _uiSystem.BattleUi.GameOver.isActiveAndEnabled;
        }

        public void SetSkillFlickOnParamView( int skillIndex, bool enabled, ParameterWindowType winType )
        {
            _parameterUIs[( int ) winType].GetSkillBox( skillIndex ).SetFlickEnabled( enabled );
        }

        public void SetUsingSkillOnLeftParamView( int skillIndex )
        {
            _uiSystem.BattleUi.ParameterView.PlayerParameter.GetSkillBox( skillIndex ).SetUsing();
        }

        public void SetUseableSkillOnParamView( int skillIndex, bool isUsable, ParameterWindowType winType )
        {
            _parameterUIs[( int ) winType].GetSkillBox( skillIndex ).SetUseable( isUsable );
        }

        public void UpdateLeftParameterView()
        {
            _uiSystem.BattleUi.ParameterView.PlayerParameter.UpdateAssignCharacterParamRender();
        }

        public void UpdateRightParameterView()
        {
            _uiSystem.BattleUi.ParameterView.EnemyParameter.UpdateAssignCharacterParamRender();
        }

        public void InitPLCommandView( PlSelectCommandState script, List<COMMAND_TAG> executableCommands )
        {
            _uiSystem.BattleUi.PlCommandWindow.RegistPLCommandScript( script );
            _uiSystem.BattleUi.PlCommandWindow.SetExecutableCommandList( executableCommands );
            _uiSystem.BattleUi.SetPlayerCommandActive( true );
        }

        public void ExitPLCommandView()
        {
            _uiSystem.BattleUi.SetPlayerCommandActive( false );
        }
    }
}