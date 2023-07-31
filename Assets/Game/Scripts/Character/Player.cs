using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static SkillsData;
using static UnityEngine.GraphicsBuffer;

public class Player : Character
{
    override public void setAnimator(ANIME_TAG animTag)
    {
        _animator.SetTrigger(_animNames[(int)animTag]);
    }
    override public void setAnimator(ANIME_TAG animTag, bool b)
    {
        _animator.SetBool(_animNames[(int)animTag], b);
    }

    /// <summary>
    /// 死亡処理。管理リストから削除し、ゲームオブジェクトを破棄します
    /// モーションのイベントフラグから呼び出します
    /// </summary>
    override public void Die()
    {
        base.Die();

        BattleManager.Instance.RemovePlayerFromList(this);
    }

    public override void SelectUseSkills(SituationType type)
    {
        KeyCode[] keyCodes = new KeyCode[Constants.EQUIPABLE_SKILL_MAX_NUM] { KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L };

        for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
        {
            if (!param.IsValidSkill(i)) continue;

            var skillType = SkillsData.data[(int)param.equipSkills[i]].Type;
            BattleUISystem.Instance.PlayerParameter.GetSkillBox(i).SetUseable(skillType == type);
            if ( skillType != type ) {
                continue;
            }

            if (Input.GetKeyUp(keyCodes[i]))
            {
                tmpParam.isUseSkills[i] = !tmpParam.isUseSkills[i];

                if (tmpParam.isUseSkills[i])
                {
                    param.consumptionActionGauge += SkillsData.data[(int)param.equipSkills[i]].Cost;
                }
                else
                {
                    param.consumptionActionGauge -= SkillsData.data[(int)param.equipSkills[i]].Cost;
                }
                BattleUISystem.Instance.PlayerParameter.GetSkillBox(i).SetFlickEnabled(tmpParam.isUseSkills[i]);
            }
        }
    }
}