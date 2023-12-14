using Frontier.Stage;
using UnityEngine;
using static Frontier.SkillsData;
using static UnityEngine.GraphicsBuffer;

namespace Frontier
{
    public class Player : Character
    {
        private bool _isPrevMoving = false;
        private Vector3 _movementDestination = Vector3.zero;

        /// <summary>
        /// �ړ����͎�t�̉۔�����s���܂�
        /// </summary>
        /// <returns>�ړ����͂̎�t��</returns>
        public bool IsAcceptableMovementOperation( float gridSize )
        {
            if( _isPrevMoving )
            {
                var diff = _movementDestination - transform.position;
                diff.y = 0;
                if(diff.sqrMagnitude <= Mathf.Pow( gridSize * Constants.ACCEPTABLE_INPUT_GRID_SIZE_RATIO, 2f ) ) return true;

                return false;
            }

            return true;
        }

        /// <summary>
        /// �v���C���[�L�����N�^�[�̈ړ����̍X�V�������s���܂�
        /// </summary>
        /// <param name="gridIndex">�L�����N�^�[�̌��ݒn�ƂȂ�O���b�h�̃C���f�b�N�X�l</param>
        /// <param name="gridInfo">�w��O���b�h�̏��</param>
        public void UpdateMove(int gridIndex, in GridInfo gridInfo)
        {
            bool toggleAnimation = false;

            // �ړ��̃O���b�h�ɑ΂��Ă̂ݖړI�n���X�V
            if (0 <= gridInfo.estimatedMoveRange)
            {
                _movementDestination = gridInfo.charaStandPos;
                tmpParam.gridIndex = gridIndex;
            }

            Vector3 dir = (_movementDestination - transform.position).normalized;
            Vector3 afterPos = transform.position + dir * Constants.CHARACTER_MOVE_SPEED * Time.deltaTime;
            Vector3 afterDir = (_movementDestination - afterPos);
            afterDir.y = 0f;
            afterDir = afterDir.normalized;
            if (Vector3.Dot(dir, afterDir) <= 0)
            {
                transform.position = _movementDestination;

                if (_isPrevMoving) toggleAnimation = true;
                _isPrevMoving = false;
            }
            else
            {
                transform.position = afterPos;
                transform.rotation = Quaternion.LookRotation(dir);

                if (!_isPrevMoving) toggleAnimation = true;
                _isPrevMoving = true;
            }

            if (toggleAnimation) AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.MOVE, _isPrevMoving);
        }

        /// <summary>
        /// ���S�����B�Ǘ����X�g����폜���A�Q�[���I�u�W�F�N�g��j�����܂�
        /// ���[�V�����̃C�x���g�t���O����Ăяo���܂�
        /// </summary>
        override public void Die()
        {
            base.Die();

            _btlMgr.RemovePlayerFromList(this);
        }

        /// <summary>
        /// �g�p�X�L����I�����܂�
        /// </summary>
        /// <param name="type">�U���A�h��A�풓�Ȃǂ̃X�L���^�C�v</param>
        override public void SelectUseSkills(SituationType type)
        {
            KeyCode[] keyCodes = new KeyCode[Constants.EQUIPABLE_SKILL_MAX_NUM] { KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L };

            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                if (!param.IsValidSkill(i)) continue;

                var skillType = SkillsData.data[(int)param.equipSkills[i]].Type;
                BattleUISystem.Instance.GetPlayerParamSkillBox(i).SetUseable(skillType == type);
                if (skillType != type)
                {
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

                    BattleUISystem.Instance.GetPlayerParamSkillBox(i).SetFlickEnabled(tmpParam.isUseSkills[i]);
                }
            }
        }
    }
}