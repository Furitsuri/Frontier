using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

public class BattleRoutinePresenter
{
    [Inject] private IUiSystem _uiSystem                        = null;
    [Inject] private BattleRoutineController _battleRoutineCtrl = null;
    [Inject] private StageController _stageCtrl                 = null;

    private Character _selectCharacter      = null;
    private Character _prevSelectCharacter  = null;

    public void Update()
    {
        UpdateBattleParameters();   // 戦闘パラメータUI更新
    }

    public void SetActiveBattleUI( bool isActive )
    {
        _uiSystem.BattleUi.gameObject.SetActive( isActive );
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
                if( bindCharacter.Params.CharacterParam.characterTag != CHARACTER_TAG.ENEMY )
                {
                    ParameterView.PlayerParameter.AssignCharacter( bindCharacter, LAYER_MASK_INDEX_PLAYER );
                    ParameterView.EnemyParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_ENEMY );
                }
                else
                {
                    ParameterView.PlayerParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_PLAYER );
                    ParameterView.EnemyParameter.AssignCharacter( bindCharacter, LAYER_MASK_INDEX_ENEMY );
                }
                break;

            case GridCursorState.MOVE:   // 移動候補選択時
                Debug.Assert( bindCharacter != null );

                ParameterView.PlayerParameter.AssignCharacter( bindCharacter, LAYER_MASK_INDEX_PLAYER );
                if( _selectCharacter != null && _selectCharacter != bindCharacter )
                {
                    ParameterView.EnemyParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_ENEMY );
                }
                _uiSystem.BattleUi.SetEnemyParameterActive( _selectCharacter != null && _selectCharacter != bindCharacter );

                break;

            default:
                // ※1フレーム中にgameObjectのアクティブ切り替えを複数回行うと正しく反映されないため、無駄があって気持ち悪いが以下の判定文を用いる
                _uiSystem.BattleUi.SetPlayerParameterActive( _selectCharacter != null && _selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER );
                _uiSystem.BattleUi.SetEnemyParameterActive( _selectCharacter != null && _selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.ENEMY );

                // パラメータ表示を更新
                if( _selectCharacter != null && _prevSelectCharacter != _selectCharacter )
                {
                    if( _selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER )
                    {
                        ParameterView.PlayerParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_PLAYER );
                    }
                    else
                    {
                        ParameterView.EnemyParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_ENEMY );
                    }
                }

                break;
        }

        // 前フレームで選択したキャラクターと現在選択しているキャラクターが異なる場合はカメラレイヤーを元に戻す
        if( _prevSelectCharacter != null && _prevSelectCharacter != _selectCharacter )
        {
            _prevSelectCharacter.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_CHARACTER );
        }

        /*
        // 選択しているキャラクターのレイヤーをパラメータUI表示のために一時的に変更
        if( _selectCharacter != null && _prevSelectCharacter != _selectCharacter )
        {
            _selectCharacter.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_PLAYER );
        }
        */

        _prevSelectCharacter = _selectCharacter;
    }
}