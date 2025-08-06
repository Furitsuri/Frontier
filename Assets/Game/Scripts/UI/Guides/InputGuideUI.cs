using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Zenject;

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
        public Constants.GuideIcon[] _icons;
        // アイコンに対する説明文
        public string _explanation;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="icon">ガイドスプライトのアイコンタイプ</param>
        /// <param name="explanation">キーに対する説明文</param>
        public InputGuide(Constants.GuideIcon[] icons, string explanation)
        {
            _icons = icons;
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
    private List<SpriteRenderer> GuideSpriteRenderers = new List<SpriteRenderer>();

    [SerializeField]
    [Header("説明テキスト")]
    private TextMeshProUGUI GuideExplanation;

    // 値保持
    public InputGuide InputGuideValue { get; private set; } 
    private HierarchyBuilderBase _hierarchyBld = null;
    // 入力ガイドの枠UI
    private RectTransform _rectTransform;
    // 子のオブジェクト
    private GameObject[] _childrenObjects;

    [Inject]
    public void Construct( HierarchyBuilderBase hierarchyBld )
    {
        _hierarchyBld = hierarchyBld;
    }

    void Awake()
    {
        // 子のオブジェクトを取得
        _childrenObjects = new GameObject[(int)ChildIndex.NUM];
        for (int i = 0; i < (int)ChildIndex.NUM; ++i)
        {
            Transform childTransform = transform.GetChild(i);

            Debug.Assert(childTransform != null, $"Not Found : Child of \"{Enum.GetValues(typeof(ChildIndex)).GetValue(i).ToString()}\".");

            _childrenObjects[i] = childTransform.gameObject;
        }

        _rectTransform = gameObject.GetComponent<RectTransform>();

        Debug.Assert(_rectTransform != null, "GetComponent of \"RectTransform\" failed.");
    }

    /// <summary>
    /// キーガイドを設定します
    /// <param name="sprites">参照するスプライト配列</param>
    /// <param name="guide">このUIに設定するガイド情報</param>
    /// </summary>
    public void Register(Sprite[] sprites, InputGuide guide)
    {
        InputGuideValue             = guide;
        for( int i = 0; i < InputGuideValue._icons.Length; ++i)
        {
            // ヒエラルキー上ではGuideSpriteRendererに対して1つのスプライトしか設定していないため、
            // 必要に応じてListに要素を加算する
            if(GuideSpriteRenderers.Count <= i)
            {
                SpriteRenderer newSpriteRenderer = Instantiate(GuideSpriteRenderers[0]);
                newSpriteRenderer.transform.SetParent(this.gameObject.transform, false);
                newSpriteRenderer.transform.SetSiblingIndex(i);

                GuideSpriteRenderers.Add(newSpriteRenderer);
            }
            GuideSpriteRenderers[i].sprite = sprites[(int)InputGuideValue._icons[i]];
        }
        GuideExplanation.text       = InputGuideValue._explanation;

        // Z軸の位置を0に設定, スケール値を1に設定
        transform.localPosition     = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);
        _rectTransform.localScale   = Vector3.one;

        // 説明文の文字の大きさなどの設定を初期化
        InitTextMeshSetting();

        // ガイド幅を調整
        AdjustWidth();
    }

    /// <summary>
    /// スプライトの描画優先度を設定します
    /// </summary>
    /// <param name="order">優先度値</param>
    public void SetSpriteSortingOrder(int order)
    {   if (GuideSpriteRenderers != null)
        {
            for (int i = 0; i < GuideSpriteRenderers.Count; ++i)
            {
                GuideSpriteRenderers[i].sortingOrder = order;
            }
        }
        else
        {
            Debug.LogError("GuideSpriteRenderer is not assigned in InputGuideUI.");
        }
    }

    /// <summary>
    /// TextMeshProのパラメータを初期化します
    /// </summary>
    private void InitTextMeshSetting()
    {
        GuideExplanation.enableAutoSizing   = true;
        GuideExplanation.fontSizeMin        = Constants.GUIDE_TEXT_MIN_SIZE;
        GuideExplanation.fontSizeMax        = Constants.GUIDE_TEXT_MAX_SIZE;
        GuideExplanation.enableWordWrapping = true;
        GuideExplanation.overflowMode       = TextOverflowModes.Truncate;
    }

    /// <summary>
    /// 表示する文字列やスプライトに応じて、ガイド幅の長さを調整します
    /// </summary>
    private void AdjustWidth()
    {
        var spriteRectTransform = _childrenObjects[(int)ChildIndex.SPRITE].GetComponent<RectTransform>();
        Debug.Assert(spriteRectTransform != null, "GetComponent of \"RectTransform in sprite\" failed.");
        var textPosX = spriteRectTransform.anchoredPosition.x + 0.5f * spriteRectTransform.rect.width * spriteRectTransform.localScale.x + Constants.SPRITE_TEXT_SPACING_ON_KEY_GUIDE;

        var textRectTransform = _childrenObjects[(int)ChildIndex.TEXT].GetComponent<RectTransform>();
        Debug.Assert(textRectTransform != null, "GetComponent of \"RectTransform in text\" failed.");
        textRectTransform.anchoredPosition = new Vector2(textPosX, 0);
        textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GuideExplanation.preferredWidth);

        var guideWidth = textPosX + textRectTransform.sizeDelta.x;
        _rectTransform.sizeDelta = new Vector2(guideWidth, _rectTransform.sizeDelta.y);
    }
}