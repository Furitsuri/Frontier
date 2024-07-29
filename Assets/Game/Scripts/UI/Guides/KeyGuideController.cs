using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace Frontier
{
    /// <summary>
    /// �L�[�K�C�h�֘A�̐�����s���܂�
    /// </summary>
    public class KeyGuideController : MonoBehaviour
    {
        /// <summary>
        /// �e�L�[�̃A�C�R��
        /// </summary>
        public enum KeyIcon : int
        {
            UP = 0,     // ��
            DOWN,       // ��
            LEFT,       // ��
            RIGHT,      // �E
            DECISION,   // ����
            CANCEL,     // �߂�

            NUM,
        }

        /// <summary>
        /// �L�[�̃A�C�R���Ƃ��̐������̍\����
        /// </summary>
        public struct KeyGuide
        {
            // �L�[�A�C�R��
            public KeyIcon type;
            // �A�C�R���ɑ΂��������
            public string explanation;
        }

        [SerializeField]
        [Header("UI�X�N���v�g")]
        private KeyGuideUI _ui = null;

        // �e�X�v���C�g�t�@�C�����̖����̔ԍ�
        string[] spriteTailString =
        // �e�v���b�g�t�H�[�����ɎQ�ƃX�v���C�g���قȂ邽�߁A�����C���f�b�N�X���قȂ�
        {
#if UNITY_EDITOR
            "_256",  // UP
            "_257",  // DOWN
            "_258",  // LEFT
            "_259",  // RIGHT
            "_120",  // DECISION
            "_179",  // CANCEL
#elif UNITY_STANDALONE_WIN
            "_10",  // UP
            "_11",  // DOWN
            "_12",  // LEFT
            "_13",  // RIGHT
            "_20",  // DECISION
            "_21",  // CANCEL
#else
#endif
        };

        // ���݂̏󋵂ɂ����āA�L���ƂȂ�L�[�Ƃ�������������ۂ̐���
        List<KeyGuide> _keyGuides;
        // �L�[�K�C�h��\������N���X
        KeyGuideUI _keyGuideUI;

        // Start is called before the first frame update
        void Start()
        {
            // �K�C�h�X�v���C�g�̓ǂݍ��݂��s���A�A�T�C������
            Sprite guideSprite = Resources.Load<Sprite>(Constants.GUIDE_SPRITE_FOLDER_PASS + Constants.GUIDE_SPRITE_FILE_NAME);
            if( guideSprite != null )
            {
                // �L�[�K�C�h�ƃX�v���C�g�̕R�Â����s��
                for (int i = 0; i < (int)KeyIcon.NUM; ++i)
                {

                }
            }
            else
            {
                Debug.Log("Failed load guide sprites!");
            }
        }

        // Update is called once per frame
        void Update()
        {
            // _keyGuideUI.UpdateUI();
        }

        /// <summary>
        /// �J�ڐ�̃L�[�K�C�h��ݒ肵�܂�
        /// </summary>
        /// <param name="guides">�\������L�[�K�C�h�̃��X�g</param>
        public void Transit( List<KeyGuide> guides )
        {
            _keyGuides = guides;

            _keyGuideUI.RegistKey(_keyGuides);
        }
    }
}