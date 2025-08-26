using Frontier.Stage;
using Frontier.Battle;
using Frontier.Entities;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class CharacterParameterPresenter : MonoBehaviour
    {
        [Header("LeftWindowParam")]
        public CharacterParameterUI PlayerParameter;        // 左側表示のパラメータUIウィンドウ

        [Header("RightWindowParam")]
        public CharacterParameterUI EnemyParameter;         // 右側表示のパラメータUIウィンドウ

        [Header("ParameterAttackDirection")]
        public ParameterAttackDirectionUI AttackDirection;  // パラメータUI間上の攻撃(回復)元から対象への表示

        private IUiSystem _uiSystem                 = null;
        private BattleRoutineController _btlRtnCtrl = null;
        private StageController _stgCtrl            = null;
        private Character _prevCharacter            = null;

        [Inject]
        public void Construct( BattleRoutineController btlRtnCtrl, StageController stgCtrl, IUiSystem uiSystem )
        {
            _btlRtnCtrl = btlRtnCtrl;
            _stgCtrl    = stgCtrl;
            _uiSystem   = uiSystem;
        }

        void Start()
        {
            PlayerParameter.gameObject.SetActive(false);
            EnemyParameter.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            Character selectCharacter = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable(_btlRtnCtrl.SelectCharacterInfo);

            var bindCharacter = _stgCtrl.GetGridCursorControllerBindCharacter();

            switch (_stgCtrl.GetGridCursorControllerState())
            {
                case Stage.GridCursorController.State.ATTACK: // 攻撃対象選択時
                    Debug.Assert(bindCharacter != null);

                    _uiSystem.BattleUi.TogglePlayerParameter(true);
                    _uiSystem.BattleUi.ToggleEnemyParameter(true);

                    // 画面構成は以下の通り
                    //   左        右
                    // PLAYER 対 ENEMY
                    // OTHER  対 ENEMY
                    // PLAYER 対 OTHER
                    if (bindCharacter.Params.CharacterParam.characterTag != CHARACTER_TAG.ENEMY)
                    {
                        PlayerParameter.SetDisplayCharacter(bindCharacter);
                        EnemyParameter.SetDisplayCharacter(selectCharacter);
                    }
                    else
                    {
                        PlayerParameter.SetDisplayCharacter(selectCharacter);
                        EnemyParameter.SetDisplayCharacter(bindCharacter);
                    }
                    break;
                
                case Stage.GridCursorController.State.MOVE:   // 移動候補選択時
                    Debug.Assert(bindCharacter != null);

                    PlayerParameter.SetDisplayCharacter(bindCharacter);
                    if (selectCharacter != null && selectCharacter != bindCharacter) EnemyParameter.SetDisplayCharacter(selectCharacter);
                    _uiSystem.BattleUi.ToggleEnemyParameter(selectCharacter != null && selectCharacter != bindCharacter);

                    break;

                default:
                    // ※1フレーム中にgameObjectのアクティブ切り替えを複数回行うと正しく反映されないため、無駄があって気持ち悪いが以下の判定文を用いる
                    _uiSystem.BattleUi.TogglePlayerParameter(selectCharacter != null && selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER);
                    _uiSystem.BattleUi.ToggleEnemyParameter(selectCharacter != null && selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.ENEMY);

                    // パラメータ表示を更新
                    if (selectCharacter != null)
                    {
                        if (selectCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER)
                        {
                            PlayerParameter.SetDisplayCharacter(selectCharacter);
                        }
                        else
                        {
                            EnemyParameter.SetDisplayCharacter(selectCharacter);
                        }
                    }

                    break;
            }

            // 前フレームで選択したキャラクターと現在選択しているキャラクターが異なる場合はカメラレイヤーを元に戻す
            if (_prevCharacter != null && _prevCharacter != selectCharacter)
            {
                _prevCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer(Constants.LAYER_NAME_CHARACTER));
            }

            // 選択しているキャラクターのレイヤーをパラメータUI表示のために一時的に変更
            if (selectCharacter != null && _prevCharacter != selectCharacter)
            {
                selectCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer(Constants.LAYER_NAME_LEFT_PARAM_WINDOW));
            }

            _prevCharacter = selectCharacter;
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
            AttackDirection.gameObject.SetActive(true);
        }
    }
}