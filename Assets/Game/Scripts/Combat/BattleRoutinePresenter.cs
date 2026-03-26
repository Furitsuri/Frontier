using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using Frontier.UI;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
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
        private Action<bool>[] SetActiveActionDirectionCallbacks;
        static private int[] _layerMaskIndex;
        public CharacterParameterPresenter CharaParamView( ParameterWindowType winType ) => _parameterPresenters[( int ) winType];

        public void Setup()
        {
            _parameterPresenters = new CharacterParameterPresenter[( int ) ParameterWindowType.NUM]
            {
                _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>( new object[] { _uiSystem.BattleUi.ParameterView.PlayerParameter, true }, false ),
                _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>( new object[] { _uiSystem.BattleUi.ParameterView.EnemyParameter, true }, false ),
            };

            _parameterUIs = new UI.CharacterParameterUI[( int )ParameterWindowType.NUM]
            {
                _uiSystem.BattleUi.ParameterView.PlayerParameter,
                _uiSystem.BattleUi.ParameterView.EnemyParameter
            };

            SetActiveActionDirectionCallbacks = new Action<bool>[( int ) ParameterWindowType.NUM]
            {
                SetActiveParamWinDirectionLeft2Right,
                SetActiveParamWinDirectionRight2Left
            };

            _layerMaskIndex = new int[]
            {
                LAYER_MASK_INDEX_PLAYER,
                LAYER_MASK_INDEX_ENEMY
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

        public void SetActiveParamWinDirectionLeft2Right( bool isActive )
        {
            _uiSystem.BattleUi.SetActiveLeft2RightDirection( isActive );
            _uiSystem.BattleUi.SetActiveRightParameterWindow( isActive );
        }

        public void SetActiveParamWinDirectionRight2Left( bool isActive )
        {
            _uiSystem.BattleUi.SetActiveLeft2RightDirection( isActive );
            _uiSystem.BattleUi.SetActiveLeftParameterWindow( isActive );
        }

        public void SetActiveActionResultExpect( bool isActive, ParameterWindowType fromWinType )
        {
            SetActiveActionDirectionCallbacks[( int ) fromWinType]( isActive ); // アクション対象指定UIの表示・非表示
            _uiSystem.BattleUi.SetActiveActionResultExpect( isActive );         // ダメージ予測表示UIの表示・非表示
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