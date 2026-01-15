

using ModestTree;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using static InputCode;
using static UnityEngine.EventSystems.StandaloneInputModule;

public class InputGuideBarUI : UiMonoBehaviour
{
    [Header( "ガイドUIのプレハブ" )]
    [SerializeField] private GameObject _guideUIPrefab;

    [Header( "背景リサイズ開始から終了までの時間" )]
    [SerializeField] private float _resizeTime = 0.33f;

    [Header( "ガイドアイコンの描画優先度" )]
    [SerializeField] private int _sortingOrder = 0;

    private RectTransform _rectTransform;       // 背景に該当するTransform
    private HorizontalLayoutGroup _layoutGrp;   // ガイドの位置調整に用いるレイアウトグループ

    public int SortingOrder => _sortingOrder;
    public float ResizeTime => _resizeTime;
    public HorizontalLayoutGroup LayoutGroup => _layoutGrp;
    public GameObject GuideUIPrefab => _guideUIPrefab;

    public void SetWidth( float nextWidth )
    {
        _rectTransform.sizeDelta = new Vector2( nextWidth, _rectTransform.sizeDelta.y );
    }

    public float GetWidth()
    {
        return _rectTransform.sizeDelta.x;
    }

    public override void Setup()
    {
        base.Setup();

        LazyInject.GetOrCreate( ref _rectTransform, () => GetComponent<RectTransform>() );
        LazyInject.GetOrCreate( ref _layoutGrp, () => GetComponent<HorizontalLayoutGroup>() );
    }
}