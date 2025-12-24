using UnityEngine;
using UnityEngine.UI;
using System.Collections.ObjectModel;
using Zenject;

public class TutorialPresenter
{
    [Inject] private IUiSystem _uiSystem = null;
    [Inject] private HierarchyBuilderBase _hierarchyBld = null;

    private TutorialUI _tutorialUI = null;
    private ReadOnlyCollection<TutorialElement> _displayContents;   // 表示内容の参照データ

    /// <summary>
    /// チュートリアルの表示内容に対する初期化を行います
    /// </summary>
    public void Init()
    {
        _tutorialUI = _uiSystem.GeneralUi.TutorialView;

        // 初期状態では非表示にする
        _tutorialUI.gameObject.SetActive( false );

        // 前ページ用Imageを生成し、向き、位置を初期化
        _tutorialUI.AssiginPrevArrowImage( ( originalObj, parentObj ) =>
        {
            Image prevArrowImg = _hierarchyBld.CreateComponentWithNestedParent<Image>( originalObj, parentObj, false );
            return prevArrowImg;
        } );
    }

    /// <summary>
    /// チュートリアルの表示を開始します
    /// </summary>
    /// <param name="tutorialIdx">表示するチュートリアルのインデックス番号</param>
    public void ShowTutorial( ReadOnlyCollection<TutorialElement> tutorialElms, int pageMaxNum, int tutorialIdx )
    {
        _tutorialUI.gameObject.SetActive( true );

        _displayContents = tutorialElms;

        ShowPage( 0 );  // 先頭ページを表示
    }

    /// <summary>
    /// チュートリアルの表示内容を切り替えます
    /// </summary>
    /// <param name="pageIdx">表示するページのインデックス番号</param>
    public void SwitchPage( int pageIdx )
    {
        ShowPage( pageIdx );
    }

    /// <summary>
    /// チュートリアルの表示を終了します
    /// </summary>
    public void Exit()
    {
        _tutorialUI.gameObject.SetActive( false );
    }

    /// <summary>
    /// 指定ページを表示します
    /// </summary>
    /// <param name="pageIdx">指定するページ番号</param>
    private void ShowPage( int pageIdx )
    {
        if( pageIdx < 0 || pageIdx >= _displayContents.Count )
        {
            Debug.LogError( "Invalid content index: " + pageIdx );
            return;
        }

        SetActivePageObjects( pageIdx, _displayContents.Count );

        // 現ページのコンテンツを取得してUIに反映
        TutorialElement currentContent = _displayContents[pageIdx];
        _tutorialUI.ApplyTutorialContent( currentContent, pageIdx, _displayContents.Count );
    }

    /// <summary>
    /// ページの矢印の表示状態を設定します
    /// </summary>
    /// <param name="pageIdx"></param>
    /// <param name="pageNum"></param>
    private void SetActivePageObjects( int pageIdx, int pageNum )
    {
        bool isExistPrevPage = ( pageIdx > 0 );
        bool isExistNextPage = ( pageIdx < pageNum - 1 );

        _tutorialUI.SetActiveArrowImages( isExistPrevPage, isExistNextPage );
    }
}