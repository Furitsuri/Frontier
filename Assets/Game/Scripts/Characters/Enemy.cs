using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class Enemy : Character
    {
        /// <summary>
        /// �v�l�^�C�v
        /// </summary>
        public enum ThinkingType
        {
            AGGERESSIVE = 0,    // �ϋɓI�Ɉړ����A�U����̌��ʂ̕]���l�������Ώۂ�_��
            WAITING,            // ���g�̍s���͈͂ɑΏۂ������Ă��Ȃ����蓮���Ȃ��B�����n�߂����AGGRESSIVE�Ɠ�������

            NUM
        }

        private ThinkingType _thikType;
        public EMAIBase EmAI { get; private set; }

        public void SetThinkType(ThinkingType type)
        {
            _thikType = type;

            // �v�l�^�C�v�ɂ����emAI�ɑ������h���N���X��ύX����
            switch (_thikType)
            {
                case ThinkingType.AGGERESSIVE:
                    EmAI = new EMAIAggressive();
                    break;
                case ThinkingType.WAITING:
                    // TODO : Wait�^�C�v���쐬����ǉ�
                    break;
                default:
                    EmAI = new EMAIBase();
                    break;
            }

            EmAI.Init(this, _btlMgr, _stageCtrl);
        }

        override public void setAnimator(AnimDatas.ANIME_CONDITIONS_TAG animTag)
        {
            _animator.SetTrigger(AnimDatas.ANIME_CONDITIONS_NAMES[(int)animTag]);
        }

        override public void setAnimator(AnimDatas.ANIME_CONDITIONS_TAG animTag, bool b)
        {
            _animator.SetBool(AnimDatas.ANIME_CONDITIONS_NAMES[(int)animTag], b);
        }

        /// <summary>
        /// ���S�����B�Ǘ����X�g����폜���A�Q�[���I�u�W�F�N�g��j�����܂�
        /// MEMO : ���[�V�����̃C�x���g�t���O����Ăяo���܂�
        /// </summary>
        public override void Die()
        {
            base.Die();

            _btlMgr.RemoveEnemyFromList(this);
        }

        /// <summary>
        /// �ړI���W�ƕW�I�L�����N�^�[�����肷��
        /// </summary>
        public (bool, bool) DetermineDestinationAndTargetWithAI()
        {
            return EmAI.DetermineDestinationAndTarget(param, tmpParam);
        }

        /// <summary>
        /// �ڕW���W�ƕW�I�L�����N�^�[���擾���܂�
        /// </summary>
        public void FetchDestinationAndTarget(out int destinationIndex, out Character targetCharacter)
        {
            destinationIndex = EmAI.GetDestinationGridIndex();
            targetCharacter = EmAI.GetTargetCharacter();
        }
    }
}