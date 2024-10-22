using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier
{

    public class KeyManager : MonoBehaviour
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

        public static KeyManager instance = null;

        private ToggleKeyCode[] _switchCodes;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            InitKeyCodes();
        }

        /// <summary>
        /// ����ΏۂƂȂ�L�[�R�[�h�����������܂�
        /// </summary>
        private void InitKeyCodes()
        {
            _switchCodes = new ToggleKeyCode[(int)Constants.KeyIcon.NUM_MAX]
            {
                ( KeyCode.UpArrow,     false, Constants.KeyIcon.ALL_CURSOR ),
                ( KeyCode.DownArrow,   false, Constants.KeyIcon.ALL_CURSOR ),
                ( KeyCode.RightArrow,  false, Constants.KeyIcon.ALL_CURSOR ),
                ( KeyCode.LeftArrow,   false, Constants.KeyIcon.ALL_CURSOR ),
                ( KeyCode.Space,       false, Constants.KeyIcon.DECISION),
                ( KeyCode.Backspace,   false, Constants.KeyIcon.CANCEL ),
                ( KeyCode.Escape,      false, Constants.KeyIcon.ESCAPE ),
                ( KeyCode.UpArrow,     false, Constants.KeyIcon.UP )
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

            new KeyGuideUI.KeyGuide(keyIcon, keyExplanation);


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

        public void ChangeKeyCodeIconAndExplanation(Constants.KeyIcon keyIcon, string keyExplanation, bool isKeyActive)
        {
            _switchCodes[(int)keyIcon].Icon         = keyIcon;
            _switchCodes[(int)keyIcon].Explanation  = keyExplanation;

            SetKeyCodeActive(keyIcon, isKeyActive);
        }
    }
}