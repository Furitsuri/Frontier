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

        // キーガイドバーの入出状態
        private Mode _mode;
        // キーガイドバーに表示するキーのスプライトとその説明文
        private List<(Sprite sprite, TextMeshProUGUI explanation)> _keys;

        private int _prevKeyCount;

        SpriteRenderer _spriteRenderer;

        private void Start()
        {
            _prevKeyCount   = 0;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// キーガイドのUIを更新します
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
                    // NEUTRAL時は何もしない
                    break;
            }
        }

        /// <summary>
        /// キーガイドを設定します
        /// </summary>
        /// <param name="keys">設定するキー</param>
        public void RegistKey( List<KeyGuideController.KeyGuide> guides )
        {
            // 登録されているキーを一度全て削除
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