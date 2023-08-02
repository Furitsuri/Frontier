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
    /// ���S�����B�Ǘ����X�g����폜���A�Q�[���I�u�W�F�N�g��j�����܂�
    /// ���[�V�����̃C�x���g�t���O����Ăяo���܂�
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

                int skillID = (int)param.equipSkills[i];
                var skillData = SkillsData.data[skillID];

                if (tmpParam.isUseSkills[i])
                {
                    // �R�X�g�����݂̃A�N�V�����Q�[�W�l���z���Ă���ꍇ�͖���
                    if (param.curActionGauge < param.consumptionActionGauge + skillData.Cost)
                    {
                        tmpParam.isUseSkills[i] = false;
                        continue;
                    }

                    param.consumptionActionGauge += skillData.Cost;

                    skillModifiedParam.AtkNum += skillData.AddAtkNum;
                    skillModifiedParam.AtkMagnification += skillData.AddAtkMag;
                    skillModifiedParam.DefMagnification += skillData.AddDefMag;
                }
                else
                {
                    param.consumptionActionGauge -= skillData.Cost;

                    skillModifiedParam.AtkNum -= skillData.AddAtkNum;
                    skillModifiedParam.AtkMagnification -= skillData.AddAtkMag;
                    skillModifiedParam.DefMagnification -= skillData.AddDefMag;
                }

                BattleUISystem.Instance.PlayerParameter.GetSkillBox(i).SetFlickEnabled(tmpParam.isUseSkills[i]);
            }
        }
    }
}