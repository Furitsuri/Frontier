using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class BattleUISystem : MonoBehaviour
    {
        public static BattleUISystem Instance { get; private set; }

        [Header("CharacterParameter")]
        public CharacterParameterPresenter ParameterView;    // キャラクターパラメータ表示

        [Header("PlayerCommand")]
        public PlayerCommandUI PLCommandWindow;         // プレイヤーの選択コマンドUI

        [Header("ConfirmTurnEnd")]
        public ConfirmTurnEndUI ConfirmTurnEnd;         // ターン終了確認UI

        [Header("DamageUI")]
        public DamageUI Damage;                         // ダメージ表記

        [Header("PhaseUI")]
        public PhaseUI Phase;                           // フェーズ表記UI

        [Header("StageClearUI")]
        public StageClearUI StageClear;                 // ステージクリアUI

        [Header("GameOver")]
        public GameOverUI GameOver;                     // ゲームオーバー画面

        void Awake()
        {
            Instance = this;
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
            Damage.gameObject.SetActive(isActive);
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
            // キャラクターの座標からカメラ(スクリーン)座標に変換
            var characterWorldPos = character.transform.position;
            var characterScreenPos = Camera.main.WorldToScreenPoint(characterWorldPos);

            Damage.transform.position = characterScreenPos;
            int absDamage = Mathf.Abs(damageValue);
            Damage.damageText.text = absDamage.ToString();
            if (damageValue < 0)
            {
                Damage.damageText.color = Color.red;
            }
            else if (0 < damageValue)
            {
                Damage.damageText.color = Color.green;
            }
            else
            {
                Damage.damageText.color = Color.white;
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