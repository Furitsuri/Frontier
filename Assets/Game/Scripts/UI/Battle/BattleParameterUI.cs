using Frontier.Stage;
using Frontier.Battle;
using Frontier.Entities;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.UI
{
    public class BattleParameterUI : UiMonoBehaviour
    {
        [Header( "LeftWindowParam" )]
        [SerializeField] private CharacterParameterUI _playerParameter;        // 左側表示のパラメータUIウィンドウ

        [Header( "RightWindowParam" )]
        [SerializeField] private CharacterParameterUI _enemyParameter;         // 右側表示のパラメータUIウィンドウ

        [Header( "Parameter_attackDirection" )]
        [SerializeField] private ParameterAttackDirectionUI _attackDirection;  // パラメータUI間上の攻撃(回復)元から対象への表示

        [Inject] private IUiSystem _uiSystem                    = null;
        [Inject] private StageController _stgCtrl               = null;

        private Character _selectCharacter      = null;
        private Character _prevSelectCharacter  = null;

        public CharacterParameterUI PlayerParameter => _playerParameter;
        public CharacterParameterUI EnemyParameter => _enemyParameter;
        public ParameterAttackDirectionUI AttackDirection => _attackDirection;

        // Update is called once per frame
        void Update()
        {
            var bindCharacter = _stgCtrl.GetBindCharacterFromGridCursor();

            switch( _stgCtrl.GetGridCursorControllerState() )
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
                        _playerParameter.AssignCharacter( bindCharacter, LAYER_MASK_INDEX_PLAYER );
                        _enemyParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_ENEMY );
                    }
                    else
                    {
                        _playerParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_PLAYER );
                        _enemyParameter.AssignCharacter( bindCharacter, LAYER_MASK_INDEX_ENEMY );
                    }
                    break;

                case GridCursorState.MOVE:   // 移動候補選択時
                    Debug.Assert( bindCharacter != null );

                    _playerParameter.AssignCharacter( bindCharacter, LAYER_MASK_INDEX_PLAYER );
                    if( _selectCharacter != null && _selectCharacter != bindCharacter )
                    {
                        _enemyParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_ENEMY );
                    }
                    _uiSystem.BattleUi.SetEnemyParameterActive( _selectCharacter != null && _selectCharacter != bindCharacter );

                    break;

                default:
                    // ※1フレーム中にgameObjectのアクティブ切り替えを複数回行うと正しく反映されないため、無駄があって気持ち悪いが以下の判定文を用いる
                    _uiSystem.BattleUi.SetPlayerParameterActive( _selectCharacter != null && _selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER );
                    _uiSystem.BattleUi.SetEnemyParameterActive( _selectCharacter != null && _selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.ENEMY );

                    // パラメータ表示を更新
                    if( _selectCharacter != null )
                    {   
                        if( _selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER )
                        {
                            _playerParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_PLAYER );
                        }
                        else
                        {
                            _enemyParameter.AssignCharacter( _selectCharacter, LAYER_MASK_INDEX_ENEMY );
                        }
                    }

                    break;
            }

            // 前フレームで選択したキャラクターと現在選択しているキャラクターが異なる場合はカメラレイヤーを元に戻す
            if( _prevSelectCharacter != null && _prevSelectCharacter != _selectCharacter )
            {
                _prevSelectCharacter.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_CHARACTER );
            }

            // 選択しているキャラクターのレイヤーをパラメータUI表示のために一時的に変更
            if( _selectCharacter != null && _prevSelectCharacter != _selectCharacter )
            {
                _selectCharacter.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_PLAYER );
            }

            _prevSelectCharacter = _selectCharacter;
        }

        public void SetSelectedCharacter( Character selectCharacter )
        {
            _selectCharacter = selectCharacter;
        }

        public override void Setup()
        {
            _playerParameter.Setup();
            _enemyParameter.Setup();
            _attackDirection.Setup();

            _playerParameter.Init();
            _enemyParameter.Init();

            _playerParameter.gameObject.SetActive( false );
            _enemyParameter.gameObject.SetActive( false );
        }

    }
}