﻿using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 入力ガイド表示における各ガイド部分です。
/// ガイドはスプライトと説明文で構成されており、ガイド自体の登録や描画幅などを決定します。
/// ガイド自体の座標はスクリプトからではなくInputGuidePresenterのInspectorでHorizontal Layout Groupを用いて設定しています。
/// </summary>
public class InputGuideUI : MonoBehaviour
{
    /// <summary>
    /// キーのアイコンとその説明文の構造体
    /// </summary>
    public struct InputGuide
    {
        // キーアイコン
        public Constants.GuideIcon _icon;
        // アイコンに対する説明文
        public string _explanation;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="icon">ガイドスプライトのアイコンタイプ</param>
        /// <param name="explanation">キーに対する説明文</param>
        public InputGuide(Constants.GuideIcon icon, string explanation)
        {
            _icon = icon;
            _explanation = explanation;
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
    private SpriteRenderer GuideSpriteRenderer;

    [SerializeField]
    [Header("説明テキスト")]
    private TextMeshProUGUI GuideExplanation;

    // 値保持
    public InputGuide InputGuideValue { get; private set; } 
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

            Debug.Assert(childTransform != null, $"Not Found : Child of \"{Enum.GetValues(typeof(ChildIndex)).GetValue(i).ToString()}\".");

            _objectChildren[i] = childTransform.gameObject;
        }

        _rectTransform = gameObject.GetComponent<RectTransform>();

        Debug.Assert(_rectTransform != null, "GetComponent of \"RectTransform\" failed.");

        _width = _rectTransform.rect.width;
    }

    /// <summary>
    /// 表示する文字列やスプライトに応じて、ガイド幅の長さを調整します
    /// </summary>
    private void AdjustWidth()
    {
        var spriteRectTransform = _objectChildren[(int)ChildIndex.SPRITE].GetComponent<RectTransform>();
        Debug.Assert(spriteRectTransform != null, "GetComponent of \"RectTransform in sprite\" failed.");
        var textPosX = spriteRectTransform.anchoredPosition.x + 0.5f * spriteRectTransform.rect.width * spriteRectTransform.localScale.x + Constants.SPRITE_TEXT_SPACING_ON_KEY_GUIDE;

        var textRectTransform = _objectChildren[(int)ChildIndex.TEXT].GetComponent<RectTransform>();
        Debug.Assert(textRectTransform != null, "GetComponent of \"RectTransform in text\" failed.");
        textRectTransform.anchoredPosition = new Vector2(textPosX, textRectTransform.anchoredPosition.y);
        textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GuideExplanation.preferredWidth);

        var guideWidth = textPosX + textRectTransform.sizeDelta.x;
        _rectTransform.sizeDelta = new Vector2(guideWidth, _rectTransform.sizeDelta.y);
    }

    /// <summary>
    /// キーガイドを設定します
    /// <param name="sprites">参照するスプライト配列</param>
    /// <param name="guide">このUIに設定するガイド情報</param>
    /// </summary>
    public void Register(Sprite[] sprites, InputGuide guide)
    {
        InputGuideValue = guide;
        GuideSpriteRenderer.sprite = sprites[(int)InputGuideValue._icon];
        var position = transform.localPosition;
        position.z = 0;
        transform.localPosition = position;
        _rectTransform.localScale = Vector3.one;
        GuideExplanation.text = InputGuideValue._explanation;

        // ガイド幅を調整
        AdjustWidth();
    }
}