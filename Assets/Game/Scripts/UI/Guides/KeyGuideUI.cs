using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Frontier
{
    public class KeyGuideUI : MonoBehaviour
    {
        public enum Mode
        {
            FADE_IN = 0,
            NEUTRAL,
            FADE_OUT,
        }

        // �L�[�K�C�h�o�[�̓��o���
        private Mode _mode;
        // �L�[�K�C�h�o�[�ɕ\������L�[�̃X�v���C�g�Ƃ��̐�����
        private List<(Sprite sprite, TextMeshProUGUI explanation)> _keys;

        private int _prevKeyCount;

        SpriteRenderer _spriteRenderer;

        private void Start()
        {
            _prevKeyCount   = 0;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// �L�[�K�C�h��UI���X�V���܂�
        /// </summary>
        public void UpdateUI()
        {
            switch( _mode )
            {
                case Mode.FADE_IN:
                    break;

                case Mode.FADE_OUT:
                    break;
                    
                default:
                    // NEUTRAL���͉������Ȃ�
                    break;
            }
        }

        /// <summary>
        /// �L�[�K�C�h��ݒ肵�܂�
        /// </summary>
        /// <param name="keys">�ݒ肷��L�[</param>
        public void RegistKey( List<KeyGuideController.KeyGuide> guides )
        {
            // �o�^����Ă���L�[����x�S�č폜
            _keys.Clear();

            // _keys = keys;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Fade()
        {
            _mode = Mode.NEUTRAL;

            if( _keys.Count < _prevKeyCount )
            {
                _mode = Mode.FADE_OUT;
            }
            else if( _prevKeyCount < _keys.Count )
            {
                _mode = Mode.FADE_IN;
            }

            // Resources.Load<Sprite>("");
        }
    }
}