using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using Frontier.UI;
using System.Collections.Generic;
using TMPro;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    public class BattleRoutinePresenter : PhasePresenterBase, IConfirmPresenter
    {
        [Inject] HierarchyBuilderBase _hierarchyBld = null;

        private CharacterParameterPresenter[] _parameterPresenters;
        private CharacterParameterUI[] _parameterUIs;
        private TextMeshProUGUI _TMPCommandName;
        static private int[] _layerMaskIndex;
        public CharacterParameterPresenter CharaParamView( ParameterWindowType winType ) => _parameterPresenters[( int ) winType];

        public void Setup()
        {
            _parameterPresenters = new CharacterParameterPresenter[( int ) ParameterWindowType.NUM]
            {
                _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>( new object[] { _uiSystem.BattleUi.ParameterView.PlayerParameter, true }, false ),
                _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>( new object[] { _uiSystem.BattleUi.ParameterView.EnemyParameter, true }, false ),
            };

            _layerMaskIndex = new int[]
            {
                LAYER_MASK_INDEX_PLAYER,
                LAYER_MASK_INDEX_ENEMY
            };

            _parameterUIs = new UI.CharacterParameterUI[( int )ParameterWindowType.NUM]
            {
                _uiSystem.BattleUi.ParameterView.PlayerParameter,
                _uiSystem.BattleUi.ParameterView.EnemyParameter
            };

            _TMPCommandName = _uiSystem.BattleUi.CommandName.GetComponentInChildren<TextMeshProUGUI>();
            NullCheck.AssertNotNull( _TMPCommandName, nameof( _TMPCommandName ) );
        }

        public void Init()
        {
            foreach( var paramPresenter in _parameterPresenters )
            {
                paramPresenter.Init();
            }
        }

        public void Update()
        {
            foreach( var paramPresenter in _parameterPresenters )
            {
                paramPresenter.Update();
            }
        }

        public void SetActiveBattleUI( bool isActive )
        {
            _uiSystem.BattleUi.gameObject.SetActive( isActive );
        }

        public void SetActiveConfirmUI( bool isActive )
        {
            _uiSystem.BattleUi.ConfirmTurnEnd.gameObject.SetActive( isActive );
        }

        public void SetActiveCommandName( bool isActive )
        {
            _uiSystem.BattleUi.CommandName.SetActive( isActive );
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

        public bool IsActiveStageClearAnimation()
        {
            return _uiSystem.BattleUi.StageClear.isActiveAndEnabled;
        }

        public bool IsActiveGameOverAnimation()
        {
            return _uiSystem.BattleUi.GameOver.isActiveAndEnabled;
        }

        public void SetCommandName( string name )
        {
            _TMPCommandName.text = name;
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

        static public int GetLayerMaskIndexFromWinType( ParameterWindowType winType )
        {
            return _layerMaskIndex[( int ) winType];
        }
    }
}