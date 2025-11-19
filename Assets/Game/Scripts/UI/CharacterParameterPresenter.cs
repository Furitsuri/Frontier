using Frontier.Stage;
using Frontier.Battle;
using Frontier.Entities;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier
{
    public class CharacterParameterPresenter : MonoBehaviour
    {
        [Header( "LeftWindowParam" )]
        public CharacterParameterUI PlayerParameter;        // 左側表示のパラメータUIウィンドウ

        [Header( "RightWindowParam" )]
        public CharacterParameterUI EnemyParameter;         // 右側表示のパラメータUIウィンドウ

        [Header( "ParameterAttackDirection" )]
        public ParameterAttackDirectionUI AttackDirection;  // パラメータUI間上の攻撃(回復)元から対象への表示

        [Inject] private IUiSystem _uiSystem                    = null;
        [Inject] private BattleRoutineController _btlRtnCtrl    = null;
        [Inject] private StageController _stgCtrl               = null;

        private Character _prevSelectCharacter = null;

        void Start()
        {
            PlayerParameter.gameObject.SetActive( false );
            EnemyParameter.gameObject.SetActive( false );
        }

        // Update is called once per frame
        void Update()
        {
            Character selectCharacter = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromDictionary( _btlRtnCtrl.SelectCharacterKey );

            var bindCharacter = _stgCtrl.GetBindCharacterFromGridCursor();

            switch( _stgCtrl.GetGridCursorControllerState() )
            {
                case GridCursorState.ATTACK: // 攻撃対象選択時
                    Debug.Assert( bindCharacter != null );

                    _uiSystem.BattleUi.TogglePlayerParameter( true );
                    _uiSystem.BattleUi.ToggleEnemyParameter( true );

                    // 画面構成は以下の通り
                    //   左        右
                    // PLAYER 対 ENEMY
                    // OTHER  対 ENEMY
                    // PLAYER 対 OTHER
                    if( bindCharacter.Params.CharacterParam.characterTag != CHARACTER_TAG.ENEMY )
                    {
                        PlayerParameter.SetDisplayCharacter( bindCharacter, LAYER_MASK_INDEX_PLAYER );
                        EnemyParameter.SetDisplayCharacter( selectCharacter, LAYER_MASK_INDEX_ENEMY );
                    }
                    else
                    {
                        PlayerParameter.SetDisplayCharacter( selectCharacter, LAYER_MASK_INDEX_PLAYER );
                        EnemyParameter.SetDisplayCharacter( bindCharacter, LAYER_MASK_INDEX_ENEMY );
                    }
                    break;

                case GridCursorState.MOVE:   // 移動候補選択時
                    Debug.Assert( bindCharacter != null );

                    PlayerParameter.SetDisplayCharacter( bindCharacter, LAYER_MASK_INDEX_PLAYER );
                    if( selectCharacter != null && selectCharacter != bindCharacter )
                    {
                        EnemyParameter.SetDisplayCharacter( selectCharacter, LAYER_MASK_INDEX_ENEMY );
                    }
                    _uiSystem.BattleUi.ToggleEnemyParameter( selectCharacter != null && selectCharacter != bindCharacter );

                    break;

                default:
                    // ※1フレーム中にgameObjectのアクティブ切り替えを複数回行うと正しく反映されないため、無駄があって気持ち悪いが以下の判定文を用いる
                    _uiSystem.BattleUi.TogglePlayerParameter( selectCharacter != null && selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER );
                    _uiSystem.BattleUi.ToggleEnemyParameter( selectCharacter != null && selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.ENEMY );

                    // パラメータ表示を更新
                    if( selectCharacter != null )
                    {   
                        if( selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER )
                        {
                            PlayerParameter.SetDisplayCharacter( selectCharacter, LAYER_MASK_INDEX_PLAYER );
                        }
                        else
                        {
                            EnemyParameter.SetDisplayCharacter( selectCharacter, LAYER_MASK_INDEX_ENEMY );
                        }
                    }

                    break;
            }

            // 前フレームで選択したキャラクターと現在選択しているキャラクターが異なる場合はカメラレイヤーを元に戻す
            if( _prevSelectCharacter != null && _prevSelectCharacter != selectCharacter )
            {
                _prevSelectCharacter.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_CHARACTER );
            }

            // 選択しているキャラクターのレイヤーをパラメータUI表示のために一時的に変更
            if( selectCharacter != null && _prevSelectCharacter != selectCharacter )
            {
                selectCharacter.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_PLAYER );
            }

            _prevSelectCharacter = selectCharacter;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
            PlayerParameter.Init();
            EnemyParameter.Init();
        }

        /// <summary>
        /// 攻撃の元から対象を示すUIを表示します
        /// </summary>
        public void ShowDirection()
        {
            AttackDirection.gameObject.SetActive( true );
        }
    }
}