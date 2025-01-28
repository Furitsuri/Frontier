using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class Enemy : Character
    {
        /// <summary>
        /// ���������܂�
        /// </summary>
        public override void Init()
        {
            base.Init();
        }

        /// <summary>
        /// ���S�����B�Ǘ����X�g����폜���A�Q�[���I�u�W�F�N�g��j�����܂�
        /// MEMO : ���[�V�����̃C�x���g�t���O����Ăяo���܂�
        /// </summary>
        override public void Die()
        {
            base.Die();

            _btlMgr.BtlCharaCdr.RemoveCharacterFromList(this);
        }

        /// <summary>
        /// �v�l�^�C�v��ݒ肵�܂�
        /// </summary>
        /// <param name="type">�ݒ肷��v�l�^�C�v</param>
        override public void SetThinkType(ThinkingType type)
        {
            _thikType = type;

            // �v�l�^�C�v�ɂ����emAI�ɑ������h���N���X��ύX����
            switch (_thikType)
            {
                case ThinkingType.AGGERESSIVE:
                    _baseAI = _hierarchyBld.InstantiateWithDiContainer<EMAIAggressive>();
                    break;
                case ThinkingType.WAITING:
                    _baseAI = _hierarchyBld.InstantiateWithDiContainer<EmAiWaitting>();
                    break;
                default:
                    _baseAI = _hierarchyBld.InstantiateWithDiContainer<EMAIBase>();
                    break;
            }

            _baseAI.Init();
        }

        /// <summary>
        /// �ړI���W�ƕW�I�L�����N�^�[�����肷��
        /// </summary>
        public (bool, bool) DetermineDestinationAndTargetWithAI()
        {
            return _baseAI.DetermineDestinationAndTarget(param, tmpParam);
        }

        /// <summary>
        /// �ڕW���W�ƕW�I�L�����N�^�[���擾���܂�
        /// </summary>
        public void FetchDestinationAndTarget(out int destinationIndex, out Character targetCharacter)
        {
            destinationIndex    = _baseAI.GetDestinationGridIndex();
            targetCharacter     = _baseAI.GetTargetCharacter();
        }
    }
}