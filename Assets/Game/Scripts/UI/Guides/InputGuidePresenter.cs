using Frontier;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using Zenject;
using static UnityEngine.EventSystems.StandaloneInputModule;

/// <summary>
/// 入力ガイド関連の表示制御を行います
/// </summary>using System.Collections.ObjectModel;
public class InputGuidePresenter : MonoBehaviour
{
    /// <summary>
    /// フェード中の各モード
    /// </summary>
    public enum FadeMode
    {
        NEUTRAL = 0,
        FADE,
    }

    [Header("ガイドUIのプレハブ")]
    [SerializeField]
    public GameObject GuideUIPrefab;

    [Header("背景リサイズ開始から終了までの時間")]
    [SerializeField]
    public float ResizeTime = 0.33f;

    // オブジェクト・コンポーネント作成クラス
    [Inject]
    private HierarchyBuilder _hierarchyBld = null;

    // キーガイドバーの入出状態
    private FadeMode _fadeMode = FadeMode.NEUTRAL;
    // 現在の背景の幅
    private float _currentBackGroundWidth = 0f;
    // ガイドが遷移する以前の背景の幅
    private float _prevTransitBackGroundWidth = 0f;
    // 更新する際に目標とする背景の幅
    private float _targetBackGroundWidth = 0f;
    // 現在の時間
    private float _fadeTime = 0f;
    // ガイド上に表示可能なスプライト群
    private Sprite[] _sprites;
    // 背景に該当するTransform
    private RectTransform _rectTransform;
    // ガイドの位置調整に用いるレイアウトグループ
    private HorizontalLayoutGroup _layoutGrp;
    // 表示するガイドUIの配列
    private InputGuideUI[] _guideUiArrray;
    // InputFacadeで管理している入力コード情報の参照
    private ReadOnlyCollection<InputCode> _inputCodes;
    // 各スプライトファイル名の末尾の番号
    private static readonly string[] spriteTailNoString =
    // 各プラットフォーム毎に参照スプライトが異なるため、末尾インデックスも異なる
    {
#if UNITY_EDITOR
            "_alpha_309",   // ALL_CURSOR
            "_alpha_315",   // VERTICAL
            "_alpha_314",   // HORIZONTAL
            "_alpha_201",   // CONFIRM
            "_alpha_260",   // CANCEL
            "_alpha_259",   // ESCAPE
            "_alpha_189",   // SUB1
            "_alpha_190",   // SUB2
            "_alpha_191",   // SUB3
            "_alpha_192",   // SUB4
#elif UNITY_STANDALONE_WIN
            "_9",   // ALL_CURSOR
            "_15",  // VERTICAL
            "_14",  // HORIZONTAL
            "_20",  // CONFIRM
            "_21",  // CANCEL
            "_27",  // ESCAPE
            "_100",  // SUB1
            "_101",  // SUB2
            "_102",  // SUB3
            "_103",  // SUB4
#else
#endif
        };

    // Start is called before the first frame update
    void Awake()
    {
        Debug.Assert( spriteTailNoString.Length == (int)Constants.GuideIcon.NUM_MAX, "ガイドアイコンにおける総登録数と総定義数が一致していません。");

        _guideUiArrray          = new InputGuideUI[(int)Constants.GuideIcon.NUM_MAX];
        _rectTransform          = GetComponent<RectTransform>();
        _layoutGrp              = GetComponent<HorizontalLayoutGroup>();

        DebugUtils.NULL_ASSERT( _hierarchyBld );
        DebugUtils.NULL_ASSERT( _rectTransform );
        DebugUtils.NULL_ASSERT( _layoutGrp );

        LoadSprites();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateFadeUI();
        UpdateActiveGuideUi();
    }

    /// <summary>
    /// ガイドUiを初期化します
    /// </summary>
    private void InitGuideUi()
    {
        for (int i = 0; i < _inputCodes.Count; ++i)
        {
            var code = _inputCodes[i];
            InputGuideUI guideUi = _hierarchyBld.CreateComponentWithNestedParent<InputGuideUI>(GuideUIPrefab, gameObject, true);
            if (guideUi == null) continue;

            InputGuideUI.InputGuide guide = new InputGuideUI.InputGuide(code.Icon, code.Explanation);
            guideUi.Register(_sprites, guide);
            _guideUiArrray[i] = guideUi;
            _guideUiArrray[i].gameObject.SetActive(code.EnableCb != null && code.EnableCb());
        }
    }

    /// <summary>
    /// ガイドUIのフェード処理を行います
    /// </summary>
    /// <returns>更新が完了したか</returns>
    private void UpdateFadeUI()
    {
        var completeUpdate = false;

        switch (_fadeMode)
        {
            case FadeMode.FADE:
                _fadeTime += Time.deltaTime;
                _currentBackGroundWidth = Mathf.Lerp(_prevTransitBackGroundWidth, _targetBackGroundWidth, _fadeTime / ResizeTime);

                if (Mathf.Abs(_targetBackGroundWidth - _currentBackGroundWidth) < Mathf.Epsilon)
                {
                    _currentBackGroundWidth = _targetBackGroundWidth;
                    completeUpdate = true;
                }

                _rectTransform.sizeDelta = new Vector2(_currentBackGroundWidth, _rectTransform.sizeDelta.y);

                break;

            default:
                // NEUTRAL時は何もしない
                break;
        }

        if( completeUpdate )
        {
            _prevTransitBackGroundWidth = _targetBackGroundWidth;
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

        for (int i = 0; i < _inputCodes.Count; ++i)
        {
            var code = _inputCodes[i];
            bool isActive = ( code.EnableCb != null && code.EnableCb() );
            if( !isToggled )
            {
                isToggled = (isActive != _guideUiArrray[i].gameObject.activeSelf);
            }
            
            _guideUiArrray[i].gameObject.SetActive( isActive );
        }

        // アクティブ状態が切替られたガイド項目があるため、フェード処理を行う
        if ( isToggled )
        {
            TransitFadeMode();
        }
    }

    /// <summary>
    /// 入力ガイドバーの背景の幅を更新します
    /// </summary>
    private float CalcurateBackGroundWidth()
    {
        // レイアウトグループの設定を反映
        var taregtWidth = _layoutGrp.padding.left + _layoutGrp.padding.right + _layoutGrp.spacing * (EvaluateActiveGuideUiCount() - 1);

        // ガイドUIのそれぞれの幅を加算
        foreach (var guideUi in _guideUiArrray)
        {
            // アクティブなオブジェクトのみを判定
            if ( !guideUi.gameObject.activeSelf ) continue;

            var inputGuideUiRectTransform = guideUi.gameObject.GetComponent<RectTransform>();
            Debug.Assert(inputGuideUiRectTransform != null, "GetComponent of \"RectTransform of InputGuideUI\" failed.");

            taregtWidth += inputGuideUiRectTransform.sizeDelta.x;
        }

        return taregtWidth;
    }

    /// <summary>
    /// 入力ガイドバーのフェード処理を行います
    /// </summary>
    private void TransitFadeMode()
    {
        _fadeMode = FadeMode.FADE;
        // ガイドの登録に合わせ、ガイドを納める背景の幅を求める
        _targetBackGroundWidth = CalcurateBackGroundWidth();
        // フェード前の背景の幅を保存
        _prevTransitBackGroundWidth = _rectTransform.sizeDelta.x;
    }

    /// <summary>
    /// スプライトのロード処理を行います
    /// </summary>
    private void LoadSprites()
    {
        _sprites = new Sprite[(int)Constants.GuideIcon.NUM_MAX];

        // ガイドスプライトの読み込みを行い、アサインする
        Sprite[] guideSprites = Resources.LoadAll<Sprite>(Constants.GUIDE_SPRITE_FOLDER_PASS + Constants.GUIDE_SPRITE_FILE_NAME);
        for (int i = 0; i < (int)Constants.GuideIcon.NUM_MAX; ++i)
        {
            string fileName = Constants.GUIDE_SPRITE_FILE_NAME + spriteTailNoString[i];

            foreach (Sprite sprite in guideSprites)
            {
                if (sprite.name == fileName)
                {
                    _sprites[i] = sprite;
                    break;
                }
            }

            if (_sprites[i] == null)
            {
                Debug.LogError("File Not Found : " + fileName);
            }
        }
    }

    /// <summary>
    /// アクティブなガイドUIの数を取得します
    /// </summary>
    /// <returns>アクティブなガイドUI数</returns>
    private int EvaluateActiveGuideUiCount()
    {
        int count = 0;

        foreach( var guide in _guideUiArrray )
        {
            if (guide.gameObject.activeSelf) ++count;
        }

        return count;
    }

    /// <summary>
    /// 全ての入力ガイドUIを無効にします
    /// </summary>
    private void ClearInputGuideUi()
    {
        foreach ( var guideUi in _guideUiArrray )
        {
            guideUi.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="inputCodes">入力可能となる情報</param>
    public void Init( InputCode[] inputCodes )
    {
        _inputCodes = Array.AsReadOnly(inputCodes);

        InitGuideUi();
    }

    /// <summary>
    /// 現在参照している入力コード情報から画面上に表示する入力ガイドを登録します
    /// </summary>
    public void RegisterInputGuides()
    {
        for( int i = 0; i < _inputCodes.Count; ++i )
        {
            var code = _inputCodes[i];

            _guideUiArrray[i].Register(_sprites, new InputGuideUI.InputGuide(code.Icon, code.Explanation));
        }

        // フェード状態の遷移
        TransitFadeMode();
    }
}