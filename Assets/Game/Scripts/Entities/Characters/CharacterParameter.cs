using Frontier.Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.Entities.Character;

namespace Frontier.Entities
{
    /// <summary>
    /// �L�����N�^�[�̃p�����[�^�̍\���̂ł�
    /// </summary>
    [Serializable]
    public struct CharacterParameter
    {
        public CHARACTER_TAG characterTag;  // �L�����N�^�[�^�O
        public int characterIndex;          // �L�����N�^�[�ԍ�
        public int MaxHP;                   // �ő�HP
        public int CurHP;                   // ����HP
        public int Atk;                     // �U����
        public int Def;                     // �h���
        public int moveRange;               // �ړ������W
        public int attackRange;             // �U�������W
        public int maxActionGauge;          // �A�N�V�����Q�[�W�ő�l
        public int curActionGauge;          // �A�N�V�����Q�[�W���ݒl
        public int recoveryActionGauge;     // �A�N�V�����Q�[�W�񕜒l
        public int consumptionActionGauge;  // �A�N�V�����Q�[�W����l
        public int initGridIndex;           // �X�e�[�W�J�n���O���b�h���W(�C���f�b�N�X)
        public Constants.Direction initDir; // �X�e�[�W�J�n������
        public SkillsData.ID[] equipSkills; // �������Ă���X�L��

        /// <summary>
        /// �A�N�V�����Q�[�W����ʂ����Z�b�g���܂�
        /// </summary>
        public void ResetConsumptionActionGauge()
        {
            consumptionActionGauge = 0;
        }

        /// <summary>
        /// �A�N�V�����Q�[�W��recoveryActionGauge�̕������񕜂��܂�
        /// ��{�I�Ɏ��^�[���J�n���ɌĂт܂�
        /// </summary>
        public void RecoveryActionGauge()
        {
            curActionGauge = Mathf.Clamp( curActionGauge + recoveryActionGauge, 0, maxActionGauge );
        }

        /// <summary>
        /// �w�肵���L�����N�^�[�^�O�ɍ��v���邩���擾���܂�
        /// </summary>
        /// <param name="tag">�w�肷��L�����N�^�[�^�O</param>
        /// <returns>���v���Ă��邩�ۂ�</returns>
        public bool IsMatchCharacterTag(CHARACTER_TAG tag)
        {
            return characterTag == tag;
        }

        /// <summary>
        /// ���S�����Ԃ��܂�
        /// </summary>
        /// <returns>���S���Ă��邩�ۂ�</returns>
        public bool IsDead()
        {
            return CurHP <= 0;
        }

        /// <summary>
        /// �w��̃X�L�����L�����ۂ���Ԃ��܂�
        /// </summary>
        /// <param name="index">�w��C���f�b�N�X</param>
        /// <returns>�L�����ۂ�</returns>
        public bool IsValidSkill(int index)
        {
            return SkillsData.ID.SKILL_NONE < equipSkills[index] && equipSkills[index] < SkillsData.ID.SKILL_NUM;
        }

        /// <summary>
        /// �w��̃X�L�����g�p�\���𔻒肵�܂�
        /// </summary>
        /// <param name="skillIdx">�X�L���̑����C���f�b�N�X�l</param>
        /// <returns>�w��X�L���̎g�p��</returns>
        public bool CanUseEquipSkill(int skillIdx, SkillsData.SituationType situationType)
        {
            if (Constants.EQUIPABLE_SKILL_MAX_NUM <= skillIdx)
            {
                Debug.Assert(false, "�w�肳��Ă���X�L���̑����C���f�b�N�X�l���X�L���̑����ő吔�𒴂��Ă��܂��B");

                return false;
            }

            int skillID = (int)equipSkills[skillIdx];
            var skillData = SkillsData.data[skillID];
            
            // ����̃V�`���G�[�V�����łȂ��ꍇ�͎g�p�s��(�U���V�`���G�[�V�������ɖh��X�L���͎g�p�o���Ȃ���)
            if( skillData.Type != situationType )
            {
                return false;
            }

            // �R�X�g�����݂̃A�N�V�����Q�[�W�l���z���Ă��Ȃ������`�F�b�N
            if (consumptionActionGauge + skillData.Cost <= curActionGauge)
            {
                return true;
            }

            return false;
        }
    }
}
