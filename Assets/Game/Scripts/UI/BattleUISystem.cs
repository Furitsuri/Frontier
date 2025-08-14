using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class BattleUISystem : MonoBehaviour
    {
        [Header("表示キャンバス")]
        [SerializeField]
        private Canvas _canvas;

        [Header("CharacterParameter")]
        public CharacterParameterPresenter ParameterView;    // キャラクターパラメータ表示

        [Header("PlayerCommand")]
        public PlayerCommandUI PlCommandWindow;         // プレイヤーの選択コマンドUI

        [Header("ConfirmTurnEnd")]
        public ConfirmTurnEndUI ConfirmTurnEnd;         // ターン終了確認UI

        [Header("DamageUI")]
        public DamageUI DamageValue;                    // ダメージ表記

        [Header("PhaseUI")]
        public PhaseUI Phase;                           // フェーズ表記UI

        [Header("StageClearUI")]
        public StageClearUI StageClear;                 // ステージクリアUI

        [Header("GameOver")]
        public GameOverUI GameOver;                     // ゲームオーバー画面

        private CombatSkillEventController _combatSkillCtrl;
        private RectTransform _rectTransform;                   // BattleUIのRectTransform
        private Camera _uiCamera;                               // UI表示用のカメラ

        [Inject]
        public void Construct(CombatSkillEventController combatSkillCtrl)
        {
            _combatSkillCtrl = combatSkillCtrl;
        }

            void Awake()
        {
            _rectTransform      = GetComponent<RectTransform>();
            var cameraObject    = GameObject.Find("UI_Camera");
            _uiCamera           = cameraObject.GetComponent<Camera>();

            DamageValue.Init(_rectTransform, _uiCamera);

            Debug.Assert(cameraObject != null);
        }

        public void TogglePlayerParameter(bool isActive)
        {
            ParameterView.PlayerParameter.gameObject.SetActive(isActive);
        }

        public void ToggleEnemyParameter(bool isActive)
        {
            ParameterView.EnemyParameter.gameObject.SetActive(isActive);
        }

        public void ToggleAttackCursorP2E(bool isActive)
        {
            ParameterView.AttackDirection.attackCursorP2E.gameObject.SetActive(isActive);
        }

        public void ToggleAttackCursorE2P(bool isActive)
        {
            ParameterView.AttackDirection.attackCursorE2P.gameObject.SetActive(isActive);
        }

        public void TogglePLCommand(bool isActive)
        {
            PlCommandWindow.gameObject.SetActive(isActive);
        }

        public void ToggleBattleExpect(bool isActive)
        {
            ParameterView.PlayerParameter.GetDiffHPText().gameObject.SetActive(isActive);
            ParameterView.EnemyParameter.GetDiffHPText().gameObject.SetActive(isActive);
        }

        public void ToggleDamageUI(bool isActive)
        {
            DamageValue.gameObject.SetActive(isActive);
        }

        public SkillBoxUI GetPlayerParamSkillBox( int index )
        {
            return ParameterView.PlayerParameter.GetSkillBox(index);
        }

        public SkillBoxUI GetEnemyParamSkillBox(int index)
        {
            return ParameterView.EnemyParameter.GetSkillBox(index);
        }

        public void SetDamageUIPosByCharaPos(Character character, int damageValue)
        {
            DamageValue.CharacterTransform  = character.transform;

            var parryNotifier = character.GetParrySkill;

            // パリィ成功時には専用の表記
            ParrySkillHandler parrySkillHdlr = _combatSkillCtrl.CurrentSkillHandler as ParrySkillHandler;
            if (parrySkillHdlr != null &&
                ( parrySkillHdlr.IsMatchResult( ParrySkillHandler.JudgeResult.SUCCESS ) ||
                  parrySkillHdlr.IsMatchResult( ParrySkillHandler.JudgeResult.JUST) ) )
            {
                DamageValue.damageText.color    = Color.yellow;
                DamageValue.damageText.text     = "DEFLECT";
            }
            else
            {
                int absDamage = Mathf.Abs(damageValue);
                DamageValue.damageText.text = absDamage.ToString();
                if (damageValue < 0)
                {
                    DamageValue.damageText.color = Color.red;
                }
                else if (0 < damageValue)
                {
                    DamageValue.damageText.color = Color.green;
                }
                else
                {
                    DamageValue.damageText.color = Color.white;
                }
            }
        }

        public void TogglePhaseUI(bool isActive, BattleRoutineController.TurnType turntype)
        {
            Phase.gameObject.SetActive(isActive);
            Phase.PhaseText[(int)turntype].gameObject.SetActive(isActive);
        }

        public void StartAnimPhaseUI()
        {
            Phase.StartAnim();
        }

        public bool IsPlayingPhaseUI()
        {
            return Phase.IsPlayingAnim();
        }

        public void ToggleConfirmTurnEnd(bool isActive)
        {
            ConfirmTurnEnd.gameObject.SetActive(isActive);
        }

        public void ApplyTestColor2ConfirmTurnEndUI(int selectIndex)
        {
            ConfirmTurnEnd.ApplyTextColor(selectIndex);
        }

        public void ToggleStageClearUI(bool isActive)
        {
            StageClear.gameObject.SetActive(isActive);
        }

        public void StartStageClearAnim()
        {
            StageClear.StartAnim();
        }

        public void ToggleGameOverUI(bool isActive)
        {
            GameOver.gameObject.SetActive(isActive);
        }

        public void StartGameOverAnim()
        {
            GameOver.StartAnim();
        }
    }
}