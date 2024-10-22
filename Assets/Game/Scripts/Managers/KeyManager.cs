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
        /// 指定のKeyCodeがtrueであれば有効,
        /// falseであれば無効を表します
        /// </summary>
        private struct ToggleKeyCode
        {
            // キーコード
            public KeyCode Code;
            // 有効・無効
            public bool Enable;
            // キーアイコン
            public Constants.KeyIcon Icon;
            // アイコンに対する説明文
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
        /// 判定対象となるキーコードを初期化します
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
        /// 現在のゲーム遷移において有効とする操作キーを、
        /// 画面上に表示するガイドUIと併せて登録します。
        /// また、そのキーを押下した際の処理をコールバックとしてfuncに登録します
        /// </summary>
        /// <param name="code">登録するキーコード</param>
        /// <param name="hash"></param>
        /// <param name="keyExplanation">キーの説明文</param>
        public void RegisterKeyCode(Constants.KeyIcon keyIcon, int/*StateHash*/ hash, string keyExplanation, Action func)
        {
            _switchCodes[(int)keyIcon].Enable = true;

            new KeyGuideUI.KeyGuide(keyIcon, keyExplanation);


        }

        /// <summary>
        /// 指定のキーの有効・無効を設定します
        /// </summary>
        /// <param name="keyIcon">設定対象のキー</param>
        /// <param name="isKeyActive">有効・無効</param>
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