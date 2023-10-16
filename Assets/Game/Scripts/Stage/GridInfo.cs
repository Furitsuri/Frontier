using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.Character;
using static Frontier.Stage.StageController;

namespace Frontier.Stage
{
    /// <summary>
    /// �O���b�h�P�ʂɂ�������
    /// </summary>
    public class GridInfo
    {
        // �L�����N�^�[�̗����ʒu���W(��)
        public Vector3 charaStandPos;
        // �ړ��j�Q�l(��)
        public int moveResist;
        // �ړ��l�̌��ς���l
        public int estimatedMoveRange;
        // �O���b�h��ɑ��݂���L�����N�^�[�̃^�C�v
        public CHARACTER_TAG characterTag;
        // �O���b�h��ɑ��݂���L�����N�^�[�̃C���f�b�N�X
        public int charaIndex;
        // �t���O���
        public BitFlag flag;
        // �� ��x�ݒ肳�ꂽ��͕ύX���邱�Ƃ��Ȃ��ϐ�

        /// <summary>
        /// ���������܂�
        /// TODO�F�X�e�[�W�̃t�@�C���Ǎ��ɂ����moveRange���͂��߂Ƃ����l��ݒ�o����悤�ɂ�����
        ///       �܂��AC# 10.0 ����͈����Ȃ��R���X�g���N�^�Œ�`�\(2023.5���_�̍ŐVUnity�o�[�W�����ł͎g�p�ł��Ȃ�)
        /// </summary>
        public void Init()
        {
            charaStandPos       = Vector3.zero;
            moveResist          = -1;
            estimatedMoveRange  = -1;
            characterTag        = CHARACTER_TAG.NONE;
            charaIndex          = -1;
            flag                = BitFlag.NONE;
        }

        /// <summary>
        /// �O���b�h��ɃL�����N�^�[�����݂��邩�ۂ���Ԃ��܂�
        /// </summary>
        /// <returns>�O���b�h��ɃL�����N�^�[�̑��݂��Ă��邩</returns>
        public bool IsExistCharacter()
        {
            return 0 <= charaIndex;
        }

        /// <summary>
        /// ���݂̒l���R�s�[���đΏۂɓn���܂�
        /// </summary>
        /// <returns>�l���R�s�[�����I�u�W�F�N�g</returns>
        public GridInfo Copy()
        {
            GridInfo info           = new GridInfo();
            info.charaStandPos      = charaStandPos;
            info.moveResist         = moveResist;
            info.estimatedMoveRange = estimatedMoveRange;
            info.characterTag       = characterTag;
            info.charaIndex         = charaIndex;
            info.flag               = flag;

            return info;
        }
    }
}