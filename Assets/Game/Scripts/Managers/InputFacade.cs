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

        // 入力ガイドの表示
        [SerializeField]
        [Header("InputGuidePresenter")]
        private InputGuidePresenter _inputGuidePresenter;

        private ToggleKeyCode[] _switchCodes;
        // 最後にキー操作をした時間の保持
        private float _operateKeyLastTime = 0.0f;

        void Awake()
        {
        }

        void Start()
        {
            InitInputCodes();
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {

        }

        /// <summary>
        /// 判定対象となる入力コードを初期化します
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

            new InputGuideUI.InputGuide(keyIcon, keyExplanation);


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

        /// <summary>
        /// 引数に指定されたアイコンに対応されているキーが押下されたかを調べます
        /// </summary>
        /// <param name="icon">指定アイコン</param>
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
        /// ユーザーがキー操作を行った際に、
        /// 短い時間で何度も同じキーが押下されたと判定されないためにインターバル時間を設けます
        /// </summary>
        /// <returns>キー操作が有効か無効か</returns>
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