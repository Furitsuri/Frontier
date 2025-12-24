using Frontier;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUI : UiMonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _headline;
    [SerializeField] private TextMeshProUGUI _explain;
    [SerializeField] private TextMeshProUGUI _pageNumberL;
    [SerializeField] private TextMeshProUGUI _pageNumberR;
    [SerializeField] private Image _image;
    [SerializeField] private Image _nextArrowImg;

    private Image _prevArrowImg;

    /// <summary>
    /// 前ページ方向の矢印画像をアサインします
    /// </summary>
    /// <param name="func"></param>
    public void AssiginPrevArrowImage( Func<GameObject, GameObject, Image> func )
    {
        if( _prevArrowImg != null ) { return; }

        LazyInject.GetOrCreate( ref _prevArrowImg, () => func( _nextArrowImg.gameObject, this.gameObject ) );

        // 位置を左右反転（X軸反転）
        RectTransform originalRect = _nextArrowImg.rectTransform;
        RectTransform dupRect = _prevArrowImg.rectTransform;
        Vector3 originalPos = originalRect.position;
        dupRect.position = new Vector3( -originalPos.x, originalPos.y, originalPos.z );

        // スケールを左右反転（Xに-1をかける）
        Vector3 scale = originalRect.localScale;
        dupRect.localScale = new Vector3( -Mathf.Abs( scale.x ), scale.y, scale.z );

        // オブジェクト名を変更
        _prevArrowImg.gameObject.name = "PrevArrowImage";
    }

    /// <summary>
    /// チュートリアルの内容をUIに反映します
    /// </summary>
    /// <param name="tutorialContent"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageNum"></param>
    public void ApplyTutorialContent( TutorialElement tutorialContent, int pageIndex, int pageNum )
    {
        _headline.text      = tutorialContent.Headline;
        _explain.text       = tutorialContent.Explain;
        _image.sprite       = tutorialContent.Image;
        _pageNumberL.text   = ( pageIndex + 1 ).ToString();
        _pageNumberR.text   = pageNum.ToString();
    }

    /// <summary>
    /// 矢印画像のアクティブ状態を設定します
    /// </summary>
    /// <param name="prevArrowActive"></param>
    /// <param name="nextArrowActive"></param>
    public void SetActiveArrowImages( bool prevArrowActive, bool nextArrowActive )
    {
        _prevArrowImg.gameObject.SetActive( prevArrowActive );
        _nextArrowImg.gameObject.SetActive( nextArrowActive );
    }
}
