using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using UnityEngine;
using Zenject;
using static Constants;
using static Frontier.UI.BattleUISystem;

namespace Frontier.Battle
{
    public class BattleRoutinePresenter : PhasePresenterBase, IConfirmPresenter
    {
        [Inject] private BattleRoutineController _battleRoutineCtrl = null;
        [Inject] private StageController _stageCtrl                 = null;

        private Character _selectCharacter = null;
        private Character _prevSelectCharacter = null;

        public void Update()
        {
            UpdateBattleParameters();   // 戦闘パラメータUI更新
        }

        public void SetActiveBattleUI( bool isActive )
        {
            _uiSystem.BattleUi.gameObject.SetActive( isActive );
        }

        public void SetActiveConfirmUI( bool isActive )
        {
            _uiSystem.BattleUi.ConfirmTurnEnd.gameObject.SetActive( isActive );
        }

        public void SetActivePlayerParameter( bool isActive )
        {
            _uiSystem.BattleUi.SetPlayerParameterActive( isActive );
        }

        public void SetActiveEnemyParameter( bool isActive )
        {
            _uiSystem.BattleUi.SetEnemyParameterActive( isActive );
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
            var ParameterView = _uiSystem.BattleUi.ParameterView;

            UI.CharacterParameterUI[] parameterUIs = new UI.CharacterParameterUI[]
            {
                ParameterView.PlayerParameter,
                ParameterView.EnemyParameter
            };

            int[] layerMaskIndex = new int[]
            {
                LAYER_MASK_INDEX_PLAYER,
                LAYER_MASK_INDEX_ENEMY
            };

            parameterUIs[(int)winType].AssignCharacter( character, layerMaskIndex[( int)winType] );
        }

        public bool IsActiveStageClearAnimation()
        {
            return _uiSystem.BattleUi.StageClear.isActiveAndEnabled;
        }

        public bool IsActiveGameOverAnimation()
        {
            return _uiSystem.BattleUi.GameOver.isActiveAndEnabled;
        }

        public void SetSkillFlickOnLeftParamView( int skillIndex, bool enabled )
        {
            _uiSystem.BattleUi.ParameterView.PlayerParameter.GetSkillBox( skillIndex ).SetFlickEnabled( enabled );
        }

        public void SetUseableSkillOnLeftParamView( int skillIndex, bool isUsable )
        {
            _uiSystem.BattleUi.GetPlayerParamSkillBox( skillIndex ).SetUseable( isUsable );
        }

        public void SetUseableSkillOnRightParamView( int skillIndex, bool isUsable )
        {
            _uiSystem.BattleUi.GetEnemyParamSkillBox( skillIndex ).SetUseable( isUsable );
        }

        private void UpdateBattleParameters()
        {
            var bindCharacter = _stageCtrl.GetBindCharacterFromGridCursor();
            var ParameterView = _uiSystem.BattleUi.ParameterView;
            _selectCharacter = _battleRoutineCtrl.BtlCharaCdr.GetSelectCharacter();

            switch( _stageCtrl.GetGridCursorControllerState() )
            {
                case GridCursorState.ATTACK: // 攻撃対象選択時
                    Debug.Assert( bindCharacter != null );

                    _uiSystem.BattleUi.SetPlayerParameterActive( true );
                    _uiSystem.BattleUi.SetEnemyParameterActive( true );

                    // 画面構成は以下の通り
                    //   左        右
                    // PLAYER 対 ENEMY
                    // OTHER  対 ENEMY
                    // PLAYER 対 OTHER
                    if( bindCharacter.GetStatusRef.characterTag != CHARACTER_TAG.ENEMY )
                    {
                        ParameterView.PlayerParameter.AssignCharacter( bindCharacter, LAYER_MASK_INDEX_PLAYER );
                        ParameterView.EnemyParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_ENEMY );
                    }
                    else
                    {
                        ParameterView.PlayerParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_PLAYER );
                        ParameterView.EnemyParameter.AssignCharacter( bindCharacter, LAYER_MASK_INDEX_ENEMY );
                    }

                    // 前フレームで選択したキャラクターと現在選択しているキャラクターが異なる場合はカメラレイヤーを元に戻す
                    if( _prevSelectCharacter != null && _prevSelectCharacter != _selectCharacter )
                    {
                        _prevSelectCharacter.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_CHARACTER );
                    }

                    break;

                case GridCursorState.MOVE:   // 移動候補選択時
                    break;

                default:
                    // MEMO : 1フレーム中にgameObjectのアクティブ切り替えを複数回行うと正しく反映されないため、無駄があって気持ち悪いが以下の判定文を用いる
                    _uiSystem.BattleUi.SetPlayerParameterActive( _selectCharacter != null && _selectCharacter.GetStatusRef.characterTag == CHARACTER_TAG.PLAYER );
                    _uiSystem.BattleUi.SetEnemyParameterActive( _selectCharacter != null && _selectCharacter.GetStatusRef.characterTag == CHARACTER_TAG.ENEMY );

                    // パラメータ表示を更新
                    if( _selectCharacter != null && _prevSelectCharacter != _selectCharacter )
                    {
                        if( _selectCharacter.GetStatusRef.characterTag == CHARACTER_TAG.PLAYER )
                        {
                            ParameterView.PlayerParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_PLAYER );
                        }
                        else
                        {
                            ParameterView.EnemyParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_ENEMY );
                        }
                    }

                    // 前フレームで選択したキャラクターと現在選択しているキャラクターが異なる場合はカメラレイヤーを元に戻す
                    if( _prevSelectCharacter != null && _prevSelectCharacter != _selectCharacter )
                    {
                        _prevSelectCharacter.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_CHARACTER );
                    }

                    break;
            }

            _prevSelectCharacter = _selectCharacter;
        }
    }
}