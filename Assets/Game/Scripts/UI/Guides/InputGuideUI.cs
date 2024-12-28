using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace Frontier
{
    public class InputGuideUI : MonoBehaviour
    {
        /// <summary>
        /// �L�[�̃A�C�R���Ƃ��̐������̍\����
        /// </summary>
        public struct InputGuide
        {
            public delegate bool InputCallBack();

            // �L�[�A�C�R��
            public Constants.KeyIcon _type;
            // �A�C�R���ɑ΂��������
            public string _explanation;
            // �L�[���������ꂽ�ۂɓ��삳����R�[���o�b�N
            public InputCallBack _callback;

            /// <summary>
            /// �R���X�g���N�^
            /// </summary>
            /// <param name="type">�K�C�h�X�v���C�g�̃A�C�R���^�C�v</param>
            /// <param name="explanation">�L�[�ɑ΂��������</param>
            public InputGuide(Constants.KeyIcon type, string explanation)
            {
                _type = type;
                _explanation = explanation;
                _callback = null;
            }

            /// <summary>
            /// �R���X�g���N�^(�����Ƀ^�v�����w��)
            /// </summary>
            /// <param name="type">�K�C�h�X�v���C�g�̃A�C�R���^�C�v</param>
            /// <param name="explanation">�L�[�ɑ΂��������</param>
            public InputGuide((Constants.KeyIcon type, string explanation) arg)
            {
                _type = arg.type;
                _explanation = arg.explanation;
                _callback = null;
            }

            /// <summary>
            /// �R���X�g���N�^(�R�[���o�b�N�������^�v���������Ɏw��)
            /// </summary>
            /// <param name="arg">�^�v������</param>
            public InputGuide((Constants.KeyIcon type, string explanation, InputCallBack callback) arg)
            {
                _type = arg.type;
                _explanation = arg.explanation;
                _callback = arg.callback;
            }
        }

        /// <summary>
        /// ���̃N���X���A�^�b�`����Unity�I�u�W�F�N�g�ɂ����āA
        /// ���̎q�ɊY������I�u�W�F�N�g�B�̃C���f�b�N�X���A���̃I�u�W�F�N�g���������̎�ނ�\���܂�
        /// UnityEditor���ŕҏW�����ꍇ�́A�K����������ύX����K�v������܂�
        /// </summary>
        private enum ChildIndex
        {
            SPRITE = 0, // �X�v���C�g�I�u�W�F�N�g
            TEXT,       // �e�L�X�g(�K�C�h�̐�����)�I�u�W�F�N�g

            NUM,
        }

        [SerializeField]
        [Header("�K�C�hUI�X�v���C�g")]
        private SpriteRenderer GuideSprite;

        [SerializeField]
        [Header("�����e�L�X�g")]
        private TextMeshProUGUI GuideExplanation;

        // ���̓K�C�h�̘gUI
        private RectTransform _rectTransform;
        // �q�̃I�u�W�F�N�g
        private GameObject[] _objectChildren;
        // �K�C�h��
        private float _width = 0f;

        void Awake()
        {
            // �q�̃I�u�W�F�N�g���擾
            _objectChildren = new GameObject[(int)ChildIndex.NUM];
            for (int i = 0; i < (int)ChildIndex.NUM; ++i)
            {
                Transform childTransform = transform.GetChild(i);

                Debug.Assert(childTransform != null, $"Not Found : Child of \"{ Enum.GetValues(typeof(ChildIndex)).GetValue(i).ToString() }\".");

                _objectChildren[i] = childTransform.gameObject;
            }

            _rectTransform = gameObject.GetComponent<RectTransform>();

            Debug.Assert( _rectTransform != null, "GetComponent of \"RectTransform\" failed.");

            _width = _rectTransform.rect.width;
        }

        void Update()
        {

        }

        /// <summary>
        /// �\�����镶�����X�v���C�g�ɉ����āA�K�C�h���̒����𒲐����܂�
        /// </summary>
        private void AdjustWidth()
        {
            var spriteRectTransform = _objectChildren[(int)ChildIndex.SPRITE].GetComponent<RectTransform>();
            Debug.Assert(spriteRectTransform != null, "GetComponent of \"RectTransform in sprite\" failed.");
            var textPosX            = spriteRectTransform.anchoredPosition.x + 0.5f * spriteRectTransform.rect.width * spriteRectTransform.localScale.x + Constants.SPRITE_TEXT_SPACING_ON_KEY_GUIDE;
            
            var textRectTransform = _objectChildren[(int)ChildIndex.TEXT].GetComponent<RectTransform>();
            Debug.Assert(textRectTransform != null, "GetComponent of \"RectTransform in text\" failed.");
            textRectTransform.anchoredPosition  = new Vector2(textPosX, textRectTransform.anchoredPosition.y);
            textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GuideExplanation.preferredWidth);

            var guideWidth = textPosX + textRectTransform.sizeDelta.x;
            _rectTransform.sizeDelta = new Vector2( guideWidth, _rectTransform.sizeDelta.y );
        }

        /// <summary>
        /// �L�[�K�C�h��ݒ肵�܂�
        /// <param name="sprites">�Q�Ƃ���X�v���C�g�z��</param>
        /// <param name="guide">����UI�ɐݒ肷��K�C�h���</param>
        /// </summary>
        public void Regist( Sprite[] sprites, InputGuide guide )
        {
            GuideSprite.sprite          = sprites[(int)guide._type];
            var position                = transform.localPosition;
            position.z                  = 0;
            transform.localPosition     = position;
            _rectTransform.localScale   = Vector3.one;
            GuideExplanation.text       = guide._explanation;

            // �K�C�h���𒲐�
            AdjustWidth();
        }
    }
}