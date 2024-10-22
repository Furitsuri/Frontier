using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Frontier
{
    /// <summary>
    /// �L�[�K�C�h�֘A�̕\��������s���܂�
    /// </summary>
    public class KeyGuidePresenter : MonoBehaviour
    {
        /// <summary>
        /// �t�F�[�h���̊e���[�h
        /// </summary>
        public enum FadeMode
        {
            NEUTRAL = 0,
            FADE,
        }

        [Header("�K�C�hUI�̃v���n�u")]
        [SerializeField]
        public GameObject GuideUIPrefab;

        [Header("�w�i���T�C�Y�J�n����I���܂ł̎���")]
        [SerializeField]
        public float ResizeTime = 0.33f;

        // �L�[�K�C�h�o�[�̓��o���
        private FadeMode _fadeMode = FadeMode.NEUTRAL;
        // �O��Ԃ̃K�C�hUI���X�g�̐�
        private int _prevGuideUIListCount = 0;
        // ���݂̔w�i�̕�
        private float _currentBackGroundWidth = 0f;
        // �K�C�h���J�ڂ���ȑO�̔w�i�̕�
        private float _prevTransitBackGroundWidth = 0f;
        // �X�V����ۂɖڕW�Ƃ���w�i�̕�
        private float _targetBackGroundWidth = 0f;
        // ���݂̎���
        private float _fadeTime = 0f;
        // �K�C�h��ɕ\���\�ȃX�v���C�g�Q
        private Sprite[] _sprites;
        // �w�i�ɊY������Transform
        private RectTransform _rectTransform;
        // �K�C�h�̈ʒu�����ɗp���郌�C�A�E�g�O���[�v
        private HorizontalLayoutGroup _layoutGrp;
        // �Q�[�����̌��݂̏󋵂ɂ�����e�L�[�K�C�h�̃��X�g
        List<KeyGuideUI.KeyGuide> _keyGuideList;
        // �Q�[�����̌��݂̏󋵂ɂ�����A���삪�L���ƂȂ�L�[�Ƃ�������������ۂ̐�����UI���X�g
        List<KeyGuideUI> _keyGuideUIList;
        // �e�X�v���C�g�t�@�C�����̖����̔ԍ�
        private static readonly string[] spriteTailNoString =
        // �e�v���b�g�t�H�[�����ɎQ�ƃX�v���C�g���قȂ邽�߁A�����C���f�b�N�X���قȂ�
        {
#if UNITY_EDITOR
            "_alpha_308",  // ALL_CURSOR
            "_alpha_250",  // UP
            "_alpha_251",  // DOWN
            "_alpha_252",  // LEFT
            "_alpha_253",  // RIGHT
            "_alpha_201",  // DECISION
            "_alpha_260",  // CANCEL
            "_alpha_259",  // ESCAPE
#elif UNITY_STANDALONE_WIN
            "_***", // ALL_CURSOR
            "_10",  // UP
            "_11",  // DOWN
            "_12",  // LEFT
            "_13",  // RIGHT
            "_20",  // DECISION
            "_21",  // CANCEL
            "_21",  // CANCEL
#else
#endif
        };

        // Start is called before the first frame update
        void Awake()
        {
            _keyGuideUIList         = new List<KeyGuideUI>();
            _rectTransform          = GetComponent<RectTransform>();
            _layoutGrp              = GetComponent<HorizontalLayoutGroup>();
            _prevGuideUIListCount   = 0;

            Debug.Assert(_rectTransform != null, "GetComponent of \"RectTransform\" failed.");
            Debug.Assert(_layoutGrp != null, "GetComponent of \"HorizontalLayoutGroup\" failed.");

            LoadSprites();
        }

        // Update is called once per frame
        void Update()
        {
            if( UpdateFadeUI() )
            {
                _prevTransitBackGroundWidth = _targetBackGroundWidth;
                _fadeMode = FadeMode.NEUTRAL;
                _fadeTime = 0f;
            }
        }

        /// <summary>
        /// �K�C�hUI�̃t�F�[�h�������s���܂�
        /// </summary>
        /// <returns>�X�V������������</returns>
        private bool UpdateFadeUI()
        {
            var completeUpdate = false;

            switch (_fadeMode)
            {
                case FadeMode.FADE:
                    _fadeTime += Time.deltaTime;
                    _currentBackGroundWidth = Mathf.Lerp(_prevTransitBackGroundWidth, _targetBackGroundWidth, _fadeTime / ResizeTime);

                    if( Mathf.Abs(_targetBackGroundWidth - _currentBackGroundWidth) < Mathf.Epsilon )
                    {
                        _currentBackGroundWidth = _targetBackGroundWidth;
                        completeUpdate = true;
                    }

                    _rectTransform.sizeDelta = new Vector2(_currentBackGroundWidth, _rectTransform.sizeDelta.y);
                    
                    break;

                default:
                    // NEUTRAL���͉������Ȃ�
                    break;
            }

            return completeUpdate;
        }

        /// <summary>
        /// �w�i�̕����X�V���܂�
        /// </summary>
        private float CalcurateBackGroundWidth()
        {
            // ���C�A�E�g�O���[�v�̐ݒ�𔽉f
            var taregtWidth = _layoutGrp.padding.left + _layoutGrp.padding.right + _layoutGrp.spacing * (_keyGuideUIList.Count - 1);

            // �K�C�hUI�̂��ꂼ��̕������Z
            foreach ( var keyGuideUI in _keyGuideUIList )
            {
                var keyGuideUIRectTransform = keyGuideUI.gameObject.GetComponent<RectTransform>();
                Debug.Assert(keyGuideUIRectTransform != null, "GetComponent of \"RectTransform of KeyGuideUI\" failed.");

                taregtWidth += keyGuideUIRectTransform.sizeDelta.x;
            }

            _rectTransform.sizeDelta = new Vector2(taregtWidth, _rectTransform.sizeDelta.y);

            return taregtWidth;
        }

        /// <summary>
        /// �K�C�h�o�[�̃t�F�[�h�������s���܂�
        /// </summary>
        private void TransitFadeMode()
        {
            _fadeMode = FadeMode.NEUTRAL;

            if ( 0 < Mathf.Abs(_keyGuideUIList.Count - _prevGuideUIListCount) )
            {
                _fadeMode = FadeMode.FADE;
                // �K�C�h�̓o�^�ɍ��킹�A�K�C�h��[�߂�w�i�̕������߂�
                _targetBackGroundWidth = CalcurateBackGroundWidth();
            }
        }

        /// <summary>
        /// �X�v���C�g�̃��[�h�������s���܂�
        /// </summary>
        void LoadSprites()
        {
            _sprites = new Sprite[(int)Constants.KeyIcon.NUM_MAX];

            // �K�C�h�X�v���C�g�̓ǂݍ��݂��s���A�A�T�C������
            Sprite[] guideSprites = Resources.LoadAll<Sprite>(Constants.GUIDE_SPRITE_FOLDER_PASS + Constants.GUIDE_SPRITE_FILE_NAME);
            for (int i = 0; i < (int)Constants.KeyIcon.NUM_MAX; ++i)
            {
                string fileName = Constants.GUIDE_SPRITE_FILE_NAME + spriteTailNoString[i];

                foreach (Sprite sprite in guideSprites)
                {
                    if (sprite.name == fileName)
                    {
                        _sprites[i] = sprite;
                        break;
                    }
                }

                if ( _sprites[i] == null )
                {
                    Debug.LogError("File Not Found : " + fileName);
                }
            }
        }

        /// <summary>
        /// �J�ڐ�̃L�[�K�C�h��ݒ肵�܂�
        /// </summary>
        /// <param name="guides">�\������L�[�K�C�h�̃��X�g</param>
        public void Transit( List<KeyGuideUI.KeyGuide> keyGuideList )
        {
            // �O��̕\���K�C�hUI����ۑ�
            _prevGuideUIListCount = _keyGuideUIList.Count;

            _keyGuideUIList.Clear();
            _keyGuideList = keyGuideList;

            // �I�u�W�F�N�g���C���X�^���X�����ēo�^
            Transform parentTransform = this.transform;
            foreach (KeyGuideUI.KeyGuide guide in _keyGuideList)
            {
                GameObject keyGuideObject = Instantiate(GuideUIPrefab);
                if( keyGuideObject != null )
                {
                    // ���̃C���X�^���X�̎q�C���X�^���X�Ƃ��Đ���
                    keyGuideObject.transform.SetParent(parentTransform);

                    KeyGuideUI keyGuideUI = keyGuideObject.GetComponent<KeyGuideUI>();
                    if (keyGuideUI == null) continue;

                    keyGuideUI.Regist( _sprites, guide );

                    _keyGuideUIList.Add(keyGuideUI);
                }
            }

            // �t�F�[�h��Ԃ̑J��
            TransitFadeMode();
        }
    }
}