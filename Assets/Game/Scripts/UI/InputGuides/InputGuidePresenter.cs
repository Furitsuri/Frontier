using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Zenject;
using static InputCode;

/// <summary>
/// 入力ガイド関連の表示制御を行います
/// </summary>
public sealed class InputGuidePresenter
{
    /// <summary>
    /// フェード中の各モード
    /// </summary>
    private enum FadeMode
    {
        NEUTRAL = 0,
        FADE,
    }

    [Inject] private IUiSystem _uiSystem                = null;
    [Inject] private HierarchyBuilderBase _hierarchyBld = null;

    private FadeMode _fadeMode              = FadeMode.NEUTRAL;                                             // キーガイドバーの入出状態
    private float _currentGuideBarWidth     = 0f;                                                           // 現在の背景の幅
    private float _prevFadeGuideBarWidth    = 0f;                                                           // ガイドが遷移する以前の背景の幅
    private float _targetGuideBarWidth      = 0f;                                                           // 更新する際に目標とする背景の幅
    private float _fadeTime                 = 0f;                                                           // 現在の時間
    private Sprite[] _sprites;                                                                              // ガイド上に表示可能なスプライト群
    private InputGuideBarUI _inputGuideBar = null;                                                          // ガイドバーUI
    private Dictionary<InputCode, InputGuideUI> _guideUIDict = new Dictionary<InputCode, InputGuideUI>();   // 入力コードとガイドUIの対応表
    private ReadOnlyCollection<InputCode> _inputCodes;                                                      // InputFacadeで管理している入力コード情報の参照
    private static readonly string[] spriteTailNoString =                                                   // 各スプライトファイル名の末尾の番号
    // 各プラットフォーム毎に参照スプライトが異なるため、末尾インデックスも異なる
    {
#if UNITY_EDITOR
            "_alpha_309",   // ALL_CURSOR
            "_alpha_315",   // VERTICAL
            "_alpha_314",   // HORIZONTAL
            "_alpha_263",   // CONFIRM(Tab)
            "_alpha_259",   // CANCEL(Esc)
            "_alpha_202",   // TOOL(Ctrl)
            "_alpha_204",   // INFO(Shift)
            "_alpha_203",   // OPTION1(Alt)
            "_alpha_201",   // OPTION2(Space)
            "_alpha_94",    // SUB1(1)
            "_alpha_95",    // SUB2(2)
            "_alpha_96",    // SUB3(3)
            "_alpha_97",    // SUB4(4)
            "_alpha_298",   // POINTER_MOVE
            "_alpha_300",   // POINTER_LEFT
            "_alpha_301",   // POINTER_RIGHT
            "_alpha_302",   // POINTER_MIDDLE
            "_alpha_119",   // DEBUG_MENU
#elif UNITY_STANDALONE_WIN
            "_9",       // ALL_CURSOR
            "_15",      // VERTICAL
            "_14",      // HORIZONTAL
            "_20",      // CONFIRM
            "_21",      // CANCEL
            "_20",      // TOOL
            "_21",      // INFO
            "_27",      // OPTION1
            "_27",      // OPTION2
            "_100",     // SUB1
            "_101",     // SUB2
            "_102",     // SUB3
            "_103",     // SUB4
            "",     // CAMERA_MOVE
            "_140",     // DEBUG
#else
#endif
    };

    public void Setup()
    {
        LazyInject.GetOrCreate( ref _inputGuideBar, () => _uiSystem.GeneralUi.InputGuideView );

        _inputGuideBar.Setup();
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="inputCodes">入力可能となる情報</param>
    public void Init( List<InputCode> inputCodes )
    {
        Debug.Assert( spriteTailNoString.Length == ( int ) GuideIcon.NUM_MAX, "ガイドアイコンにおける総登録数と総定義数が一致していません。" );

        _inputCodes = inputCodes.AsReadOnly();
        LoadSprites();
    }

    public void Update()
    {
        UpdateFadeUI();
        UpdateActiveGuideUi();
    }

    /// <summary>
    /// 現在参照している入力コード情報から画面上に表示する入力ガイドを登録します
    /// </summary>
    public void RegisterInputGuides()
    {
        // 登録済みのガイドUIを全て削除
        _inputGuideBar.DestroyChildren();
        _guideUIDict.Clear();

        foreach( var code in _inputCodes )
        {
            if( code.Icons == null || code.Icons.Length == 0 ) { continue; }

            InputGuideUI guideUi = _hierarchyBld.CreateComponentWithNestedParent<InputGuideUI>( _inputGuideBar.GuideUIPrefab, _inputGuideBar.gameObject, true );
            if( guideUi == null ) { continue; }
            guideUi.Setup();
            guideUi.SetSpriteSortingOrder( _inputGuideBar.SortingOrder );
            _guideUIDict.Add( code, guideUi );
            
            _guideUIDict[code].Register( _sprites, new InputGuideUI.InputGuide( code.Icons, code.Explanation ) );
            _guideUIDict[code].gameObject.SetActive( IsActiveGuideUi( code.EnableCbs ) );
        }

        TransitFadeMode();  // フェード状態の遷移
        _inputGuideBar.gameObject.SetActive( EvaluateActiveGuideUiCount() > 0 ); // ガイドUIが1つでもあればガイドバーを表示

        
    }

    /// <summary>
    /// ガイドUIのフェード処理を行います
    /// </summary>
    /// <returns>更新が完了したか</returns>
    private void UpdateFadeUI()
    {
        var completeUpdate = false;

        switch( _fadeMode )
        {
            case FadeMode.FADE:
                _fadeTime += DeltaTimeProvider.DeltaTime;
                _currentGuideBarWidth = Mathf.Lerp( _prevFadeGuideBarWidth, _targetGuideBarWidth, _fadeTime / _inputGuideBar.ResizeTime );

                if( Mathf.Abs( _targetGuideBarWidth - _currentGuideBarWidth ) < Mathf.Epsilon )
                {
                    _currentGuideBarWidth = _targetGuideBarWidth;
                    completeUpdate = true;
                }

                _inputGuideBar.SetWidth( _currentGuideBarWidth );

                break;

            default:
                // NEUTRAL時は何もしない
                break;
        }

        if( completeUpdate )
        {
            _prevFadeGuideBarWidth = _targetGuideBarWidth;
            _fadeMode = FadeMode.NEUTRAL;
            _fadeTime = 0f;
        }
    }

    /// <summary>
    /// ガイドUIのアクティブ状態を更新します
    /// </summary>
    private void UpdateActiveGuideUi()
    {
        bool isToggled = false;

        foreach( var guideUI in _guideUIDict )
        {
            bool isActive = IsActiveGuideUi( guideUI.Key.EnableCbs );
            if( isToggled == false )
            {
                isToggled = ( isActive != guideUI.Value.gameObject.activeSelf );
            }
            guideUI.Value.gameObject.SetActive( isActive );
        }

        // アクティブ状態が切替られたガイド項目があるため、フェード処理を行う
        if( isToggled )
        {
            TransitFadeMode();
        }
    }

    /// <summary>
    /// 入力ガイドバーのフェード処理を行います
    /// </summary>
    private void TransitFadeMode()
    {
        _fadeMode = FadeMode.FADE;
        
        _targetGuideBarWidth    = CalcurateGuideBarWidth();     // 現在のガイドの登録内容に合わせ、ガイドを納める背景の幅を求める
        _prevFadeGuideBarWidth  = _inputGuideBar.GetWidth();    // フェード前の背景の幅を保存
    }

    /// <summary>
    /// スプライトのロード処理を行います
    /// </summary>
    private void LoadSprites()
    {
        _sprites = new Sprite[( int ) GuideIcon.NUM_MAX];

        // ガイドスプライトの読み込みを行い、アサインする
        Sprite[] guideSprites = Resources.LoadAll<Sprite>( Constants.GUIDE_SPRITE_FOLDER_PASS + Constants.GUIDE_SPRITE_FILE_NAME );
        for( int i = 0; i < ( int ) GuideIcon.NUM_MAX; ++i )
        {
            string fileName = Constants.GUIDE_SPRITE_FILE_NAME + spriteTailNoString[i];

            foreach( Sprite sprite in guideSprites )
            {
                if( sprite.name == fileName )
                {
                    _sprites[i] = sprite;
                    break;
                }
            }

            if( _sprites[i] == null )
            {
                LogHelper.LogError( "File Not Found : " + fileName );
            }
        }
    }

    /// <summary>
    /// ガイドUIをアクティブにするか判定します
    /// </summary>
    /// <param name="enableCbs"></param>
    /// <returns></returns>
    private bool IsActiveGuideUi( EnableCallback[] enableCbs )
    {
        return enableCbs != null && !Methods.AllMatch( enableCbs, arg => ( !arg() ) );
    }

    /// <summary>
    /// アクティブなガイドUIの数を取得します
    /// </summary>
    /// <returns>アクティブなガイドUI数</returns>
    private int EvaluateActiveGuideUiCount()
    {
        int count = 0;

        foreach( var guideUI in _guideUIDict )
        {
            if( guideUI.Value.gameObject.activeSelf ) { ++count; }
        }

        return count;
    }

    /// <summary>
    /// 入力ガイドバーの背景の幅を更新します
    /// </summary>
    /// <returns>更新後のガイドバーの幅</returns>
    private float CalcurateGuideBarWidth()
    {
        // レイアウトの更新を行ってから計算する
        Canvas.ForceUpdateCanvases();

        // レイアウトグループの設定を反映
        var taregtWidth = _inputGuideBar.LayoutGroup.padding.left + _inputGuideBar.LayoutGroup.padding.right + _inputGuideBar.LayoutGroup.spacing * ( EvaluateActiveGuideUiCount() - 1 );

        // ガイドUIのそれぞれの幅を加算
        foreach( var guideUI in _guideUIDict )
        {
            if( !guideUI.Value.gameObject.activeSelf ) { continue; }

            var inputGuideUiRectTransform = guideUI.Value.gameObject.GetComponent<RectTransform>();
            Debug.Assert( inputGuideUiRectTransform != null, "GetComponent of \"RectTransform of InputGuideUI\" failed." );

            taregtWidth += inputGuideUiRectTransform.sizeDelta.x;
        }

        return taregtWidth;
    }
}