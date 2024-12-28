using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier
{
    public class InputFacade : MonoBehaviour
    {
        /// <summary>
        /// �w���KeyCode��true�ł���ΗL��,
        /// false�ł���Ζ�����\���܂�
        /// </summary>
        private struct ToggleKeyCode
        {
            // �L�[�R�[�h
            public KeyCode Code;
            // �L���E����
            public bool Enable;
            // �L�[�A�C�R��
            public Constants.KeyIcon Icon;
            // �A�C�R���ɑ΂��������
            public string Explanation;
            // 
            public Action CallbackFunc;

            public ToggleKeyCode(KeyCode code, bool enable, Constants.KeyIcon icon)
            {
                Code            = code;
                Enable          = enable;
                Icon            = icon;
                Explanation     = string.Empty;
                CallbackFunc    = null;
            }

            public static implicit operator ToggleKeyCode((KeyCode, bool, Constants.KeyIcon) tuple)
            {
                return new ToggleKeyCode(tuple.Item1, tuple.Item2, tuple.Item3);
            }
        }

        // ���̓K�C�h�̕\��
        [SerializeField]
        [Header("InputGuidePresenter")]
        private InputGuidePresenter _inputGuidePresenter;

        private ToggleKeyCode[] _switchCodes;
        // �Ō�ɃL�[������������Ԃ̕ێ�
        private float _operateKeyLastTime = 0.0f;

        void Awake()
        {
        }

        void Start()
        {
            InitInputCodes();
        }

        /// <summary>
        /// ���������܂�
        /// </summary>
        public void Init()
        {

        }

        /// <summary>
        /// ����ΏۂƂȂ���̓R�[�h�����������܂�
        /// </summary>
        private void InitInputCodes()
        {
            _switchCodes = new ToggleKeyCode[(int)Constants.KeyIcon.NUM_MAX]
            {
                ( KeyCode.UpArrow,     false, Constants.KeyIcon.ALL_CURSOR ),
                ( KeyCode.LeftArrow,   false, Constants.KeyIcon.VERTICAL_CURSOR ),
                ( KeyCode.UpArrow,     false, Constants.KeyIcon.HORIZONTAL_CURSOR ),
                ( KeyCode.Space,       false, Constants.KeyIcon.DECISION),
                ( KeyCode.Backspace,   false, Constants.KeyIcon.CANCEL ),
                ( KeyCode.Escape,      false, Constants.KeyIcon.ESCAPE )
            };
        }

        /// <summary>
        /// ���݂̃Q�[���J�ڂɂ����ėL���Ƃ��鑀��L�[���A
        /// ��ʏ�ɕ\������K�C�hUI�ƕ����ēo�^���܂��B
        /// �܂��A���̃L�[�����������ۂ̏������R�[���o�b�N�Ƃ���func�ɓo�^���܂�
        /// </summary>
        /// <param name="code">�o�^����L�[�R�[�h</param>
        /// <param name="hash"></param>
        /// <param name="keyExplanation">�L�[�̐�����</param>
        public void RegisterKeyCode(Constants.KeyIcon keyIcon, int/*StateHash*/ hash, string keyExplanation, Action func)
        {
            _switchCodes[(int)keyIcon].Enable = true;

            new InputGuideUI.InputGuide(keyIcon, keyExplanation);


        }

        /// <summary>
        /// �w��̃L�[�̗L���E������ݒ肵�܂�
        /// </summary>
        /// <param name="keyIcon">�ݒ�Ώۂ̃L�[</param>
        /// <param name="isKeyActive">�L���E����</param>
        public void SetKeyCodeActive(Constants.KeyIcon keyIcon, bool isKeyActive)
        {
            _switchCodes[(int)keyIcon].Enable = isKeyActive;
        }

        /// <summary>
        /// �����Ɏw�肳�ꂽ�A�C�R���ɑΉ�����Ă���L�[���������ꂽ���𒲂ׂ܂�
        /// </summary>
        /// <param name="icon">�w��A�C�R��</param>
        /// <returns></returns>
        public bool IsInputKey( Constants.KeyIcon icon )
        {
            switch( icon )
            {
                case Constants.KeyIcon.ALL_CURSOR:
                    return true;
                case Constants.KeyIcon.VERTICAL_CURSOR:
                    return true;
                case Constants.KeyIcon.HORIZONTAL_CURSOR:
                    return true;
                case Constants.KeyIcon.DECISION:
                    return true;
                case Constants.KeyIcon.CANCEL:
                    return true;
                case Constants.KeyIcon.ESCAPE:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyIcon"></param>
        /// <param name="keyExplanation"></param>
        /// <param name="isKeyActive"></param>
        public void ChangeKeyCodeIconAndExplanation(Constants.KeyIcon keyIcon, string keyExplanation, bool isKeyActive)
        {
            _switchCodes[(int)keyIcon].Icon         = keyIcon;
            _switchCodes[(int)keyIcon].Explanation  = keyExplanation;

            SetKeyCodeActive(keyIcon, isKeyActive);
        }

        /// <summary>
        /// ���[�U�[���L�[������s�����ۂɁA
        /// �Z�����Ԃŉ��x�������L�[���������ꂽ�Ɣ��肳��Ȃ����߂ɃC���^�[�o�����Ԃ�݂��܂�
        /// </summary>
        /// <returns>�L�[���삪�L����������</returns>
        private bool OperateKeyControl()
        {
            if ( Constants.OPERATE_KET_INTERVAL <= Time.time - _operateKeyLastTime )
            {
                _operateKeyLastTime = Time.time;

                return true;
            }

            return false;
        }
    }
}