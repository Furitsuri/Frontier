using UnityEngine;
using UnityEngine.UI;
using System.Collections.ObjectModel;
using TMPro;
using System;
using System.Collections.Generic;
using Frontier;
using Zenject;

public class TutorialPresenter : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _headline;
    [SerializeField]
    private TextMeshProUGUI _explain;
    [SerializeField]
    private TextMeshProUGUI _pageNumberL;
    [SerializeField]
    private TextMeshProUGUI _pageNumberR;
    [SerializeField]
    private Image _image;
    [SerializeField]
    private Image _nextArrowImg;
    private Image _prevArrowImg;

    private HierarchyBuilderBase _hierarchyBld = null;

    // 表示内容の参照データ
    private ReadOnlyCollection<TutorialElement> _displayContents;

    [Inject]
    public void Construct(HierarchyBuilderBase hierarchyBld)
    {
        _hierarchyBld = hierarchyBld;
    }

    /// <summary>
    /// チュートリアルの表示内容に対する初期化を行います
    /// </summary>
    public void Init()
    {
        // 初期状態では非表示にする
        gameObject.SetActive(false);

        // 前ページ用Imageを生成し、向き、位置を初期化
        InitPrevArrowImage();
    }

    /// <summary>
    /// チュートリアルの表示を開始します
    /// </summary>
    /// <param name="tutorialIdx">表示するチュートリアルのインデックス番号</param>
    public void ShowTutorial( ReadOnlyCollection<TutorialElement> tutorialElms, int pageMaxNum, int tutorialIdx )
    {
        // 先頭ページを表示
        _displayContents = tutorialElms;
        ShowPage( 0 );

        gameObject.SetActive(true);
    }

    /// <summary>
    /// チュートリアルの表示内容を切り替えます
    /// </summary>
    /// <param name="pageIdx">表示するページのインデックス番号</param>
    public void SwitchPage( int pageIdx )
    {
        ShowPage(pageIdx);
    }

    /// <summary>
    /// チュートリアルの表示を終了します
    /// </summary>
    public void Exit()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 前ページへの遷移が可能であることを指し示すImageを生成し、向き、位置を初期化します
    /// </summary>
    private void InitPrevArrowImage()
    {
        if (_prevArrowImg == null)
        {
            _prevArrowImg = _hierarchyBld.CreateComponentWithNestedParent<Image>(_nextArrowImg.gameObject, this.gameObject, false);
            if (_prevArrowImg)
            {
                // 位置を左右反転（X軸反転）
                RectTransform originalRect = _nextArrowImg.rectTransform;
                RectTransform dupRect = _prevArrowImg.rectTransform;
                Vector3 originalPos = originalRect.position;
                dupRect.position = new Vector3(-originalPos.x, originalPos.y, originalPos.z);
                // スケールを左右反転（Xに-1をかける）
                Vector3 scale = originalRect.localScale;
                dupRect.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
                // オブジェクト名を変更
                _prevArrowImg.gameObject.name = "PrevArrowImage";
            }
        }
    }

    /// <summary>
    /// 指定ページを表示します
    /// </summary>
    /// <param name="pageIdx">指定するページ番号</param>
    private void ShowPage( int pageIdx )
    {
        if (pageIdx < 0 || pageIdx >= _displayContents.Count)
        {
            Debug.LogError("Invalid content index: " + pageIdx);
            return;
        }

        SetActivePageObjects(pageIdx);

        // 現在のコンテンツを取得
        TutorialElement currentContent = _displayContents[pageIdx];
        // UI要素に内容を設定
        _headline.text      = currentContent.Headline;
        _explain.text       = currentContent.Explain;
        _image.sprite       = currentContent.Image;
        _pageNumberL.text   = (pageIdx + 1).ToString();
        _pageNumberR.text   = _displayContents.Count.ToString();
    }

    /// <summary>
    /// ページの矢印の表示状態を設定します
    /// </summary>
    /// <param name="pageIdx">ページのインデックス番号</param>
    private void SetActivePageObjects( int pageIdx )
    {
        if (_displayContents.Count == 1)
        {   
            // ページ数が1の場合は、前ページの矢印を非表示にする
            _prevArrowImg.gameObject.SetActive(false);
            _nextArrowImg.gameObject.SetActive(false);
        }
        else if (pageIdx == 0)
        {
            // 最初のページの場合は、前ページの矢印を非表示にする
            _prevArrowImg.gameObject.SetActive(false);
            _nextArrowImg.gameObject.SetActive(true);
        }
        else if (pageIdx == _displayContents.Count - 1)
        {
            // 最後のページの場合は、次ページの矢印を非表示にする
            _prevArrowImg.gameObject.SetActive(true);
            _nextArrowImg.gameObject.SetActive(false);
        }
        else
        {
            // 中間ページの場合は、両方の矢印を表示する
            _prevArrowImg.gameObject.SetActive(true);
            _nextArrowImg.gameObject.SetActive(true);
        }
    }
}