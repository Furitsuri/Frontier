using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class BattleUISystem : MonoBehaviour
    {
        public static BattleUISystem Instance { get; private set; }

        [Header("�\���L�����o�X")]
        [SerializeField]
        private Canvas _canvas;

        [Header("CharacterParameter")]
        public CharacterParameterPresenter ParameterView;    // �L�����N�^�[�p�����[�^�\��

        [Header("PlayerCommand")]
        public PlayerCommandUI PLCommandWindow;         // �v���C���[�̑I���R�}���hUI

        [Header("ConfirmTurnEnd")]
        public ConfirmTurnEndUI ConfirmTurnEnd;         // �^�[���I���m�FUI

        [Header("DamageUI")]
        public DamageUI DamageValue;                    // �_���[�W�\�L

        [Header("PhaseUI")]
        public PhaseUI Phase;                           // �t�F�[�Y�\�LUI

        [Header("StageClearUI")]
        public StageClearUI StageClear;                 // �X�e�[�W�N���AUI

        [Header("GameOver")]
        public GameOverUI GameOver;                     // �Q�[���I�[�o�[���

        // BattleUI��RectTransform
        private RectTransform _rectTransform;

        // UI�\���p�̃J����
        private Camera _uiCamera;

        void Awake()
        {
            Instance            = this;
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
            PLCommandWindow.gameObject.SetActive(isActive);
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

            // �p���B�������ɂ͐�p�̕\�L
            if (character.ParryResult == SkillParryController.JudgeResult.SUCCESS ||
                character.ParryResult == SkillParryController.JudgeResult.JUST)
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

        public void TogglePhaseUI(bool isActive, BattleManager.TurnType turntype)
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