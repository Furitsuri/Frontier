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
        private KeyGuideUI _keyGuideUI = null;

        // �K�C�h��ɕ\���\�ȃX�v���C�g�Q
        private Sprite[] sprites;

        // �e�X�v���C�g�t�@�C�����̖����̔ԍ�
        string[] spriteTailNoString =
        // �e�v���b�g�t�H�[�����ɎQ�ƃX�v���C�g���قȂ邽�߁A�����C���f�b�N�X���قȂ�
        {
#if UNITY_EDITOR
            "_alpha_250",  // UP
            "_alpha_251",  // DOWN
            "_alpha_252",  // LEFT
            "_alpha_253",  // RIGHT
            "_alpha_120",  // DECISION
            "_alpha_179",  // CANCEL
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

        // �Q�[�����̌��݂̏󋵂ɂ�����A���삪�L���ƂȂ�L�[�Ƃ�������������ۂ̐����̃��X�g
        List<KeyGuide> _keyGuides;

        // Start is called before the first frame update
        void Start()
        {
            LoadSprites();
        }

        // Update is called once per frame
        void Update()
        {
            _keyGuideUI.UpdateUI();
        }

        /// <summary>
        /// �X�v���C�g�̃��[�h�������s���܂�
        /// </summary>
        void LoadSprites()
        {
            sprites = new Sprite[(int)KeyIcon.NUM];

            // �K�C�h�X�v���C�g�̓ǂݍ��݂��s���A�A�T�C������
            Sprite[] guideSprites = Resources.LoadAll<Sprite>(Constants.GUIDE_SPRITE_FOLDER_PASS + Constants.GUIDE_SPRITE_FILE_NAME);
            for (int i = 0; i < (int)KeyIcon.NUM; ++i)
            {
                string fileName = Constants.GUIDE_SPRITE_FILE_NAME + spriteTailNoString[i];

                foreach (Sprite sprite in guideSprites)
                {
                    if (sprite.name == fileName)
                    {
                        sprites[i] = sprite;
                        break;
                    }
                }

                if ( sprites[i] == null )
                {
                    Debug.LogError("File Not Found : " + fileName);
                }
            }
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