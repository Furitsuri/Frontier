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
        /// キーのアイコンとその説明文の構造体
        /// </summary>
        public struct InputGuide
        {
            public delegate bool InputCallBack();

            // キーアイコン
            public Constants.KeyIcon _type;
            // アイコンに対する説明文
            public string _explanation;
            // キーが押下された際に動作させるコールバック
            public InputCallBack _callback;

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="type">ガイドスプライトのアイコンタイプ</param>
            /// <param name="explanation">キーに対する説明文</param>
            public InputGuide(Constants.KeyIcon type, string explanation)
            {
                _type = type;
                _explanation = explanation;
                _callback = null;
            }

            /// <summary>
            /// コンストラクタ(引数にタプルを指定)
            /// </summary>
            /// <param name="type">ガイドスプライトのアイコンタイプ</param>
            /// <param name="explanation">キーに対する説明文</param>
            public InputGuide((Constants.KeyIcon type, string explanation) arg)
            {
                _type = arg.type;
                _explanation = arg.explanation;
                _callback = null;
            }

            /// <summary>
            /// コンストラクタ(コールバックを内包したタプルを引数に指定)
            /// </summary>
            /// <param name="arg">タプル引数</param>
            public InputGuide((Constants.KeyIcon type, string explanation, InputCallBack callback) arg)
            {
                _type = arg.type;
                _explanation = arg.explanation;
                _callback = arg.callback;
            }
        }

        /// <summary>
        /// このクラスをアタッチしたUnityオブジェクトにおいて、
        /// その子に該当するオブジェクト達のインデックスが、何のオブジェクトを示すかの種類を表します
        /// UnityEditor側で編集した場合は、必ずこちらも変更する必要があります
        /// </summary>
        private enum ChildIndex
        {
            SPRITE = 0, // スプライトオブジェクト
            TEXT,       // テキスト(ガイドの説明文)オブジェクト

            NUM,
        }

        [SerializeField]
        [Header("ガイドUIスプライト")]
        private SpriteRenderer GuideSprite;

        [SerializeField]
        [Header("説明テキスト")]
        private TextMeshProUGUI GuideExplanation;

        // 入力ガイドの枠UI
        private RectTransform _rectTransform;
        // 子のオブジェクト
        private GameObject[] _objectChildren;
        // ガイド幅
        private float _width = 0f;

        void Awake()
        {
            // 子のオブジェクトを取得
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
        /// 表示する文字列やスプライトに応じて、ガイド幅の長さを調整します
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
        /// キーガイドを設定します
        /// <param name="sprites">参照するスプライト配列</param>
        /// <param name="guide">このUIに設定するガイド情報</param>
        /// </summary>
        public void Regist( Sprite[] sprites, InputGuide guide )
        {
            GuideSprite.sprite          = sprites[(int)guide._type];
            var position                = transform.localPosition;
            position.z                  = 0;
            transform.localPosition     = position;
            _rectTransform.localScale   = Vector3.one;
            GuideExplanation.text       = guide._explanation;

            // ガイド幅を調整
            AdjustWidth();
        }
    }
}