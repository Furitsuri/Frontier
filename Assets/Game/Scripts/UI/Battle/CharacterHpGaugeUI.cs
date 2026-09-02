using Frontier.Entities;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// キャラクター頭上に常時表示するHPゲージです。
/// 角丸の黒い下地(Background)の上に、キャラクタータグに応じた色のグラデーション付きゲージバー(Fill)を重ね、
/// Fillのwidthを現在HP割合に応じて変化させることでHPを表現します。
/// FillはBackgroundよりわずかに小さいサイズで生成するため、満タン時も下地の黒が縁に少しだけ見えます。
/// また、攻撃対象選択中はSetPredictedDamage()で予測ダメージ分の範囲を表示できます。
/// Fill自体は減少後のHP位置まで静的に縮め、その手前の「減少分」の範囲にはFillと同色の
/// 別Imageを重ねてアルファ値を点滅させることで、伸縮ではなくフェードで減少量を表現します。
/// 予測ダメージで対象のHPが0以下になる場合は、ゲージ左端に撃破確定アイコン(赤いバッジ+
/// ばってん目のドクロ、Assets/Game/Textures/UI/DefeatIcon.png)を表示します。
/// </summary>
public class CharacterHpGaugeUI : UiMonoBehaviour
{
    [Header( "撃破確定アイコン" )]
    [SerializeField] private Sprite _defeatIconSprite;

    private RectTransform _btlUiRectTransform;
    private Camera _btlUiCamera;
    private RectTransform _selfRectTransform;
    private Image _fillImage;
    private Image _predictedDamageImage;
    private Image _defeatIconImage;
    private int _predictedDamageAmount;
    public Character TargetCharacter { get; private set; }

    /// <summary>
    /// 背景・前景バーの子Imageを生成します。
    /// 戦闘UI(BattleUI)自体が非アクティブな間はSetActive(true)を呼んでもAwake()が発火しないため、
    /// Awake()には頼らずShowFor()から明示的に呼び出します。
    /// </summary>
    private void EnsureBuilt()
    {
        if( _fillImage != null ) { return; }

        _selfRectTransform = GetComponent<RectTransform>();
        _selfRectTransform.sizeDelta = new Vector2( Constants.HP_GAUGE_WIDTH, Constants.HP_GAUGE_HEIGHT );

        // 下地(黒、角丸のみ。グラデーションなし)
        CreateBackgroundImage();

        // ゲージバー(角丸+グラデーション。下地に対してHP_GAUGE_FILL_SIZE_RATIO倍のサイズで中央に配置することで、
        // 満タン時も下地の黒縁がわずかに見えるようにする
        _fillImage = CreateFillImage();

        // 予測ダメージ表示(Fillの子とすることで、Fillの見た目上のサイズ=MaxHP幅を基準にした0〜1の割合で
        // そのままアンカーを指定できる。Image.Type.Filledによる描画クロップは子オブジェクトのRectTransform
        // 計算には影響しないため、Fillのfillamountを縮めた状態でもこの子は正しい位置に描画される)
        _predictedDamageImage = CreatePredictedDamageImage();

        // 撃破確定アイコン(ゲージ本体=transformの子。ゲージ左端に固定サイズのバッジとして表示するため、
        // fillamountで伸縮するFillではなくゲージ全体の左端を基準に配置する)
        _defeatIconImage = CreateDefeatIconImage();
    }

    void Update()
    {
        if( TargetCharacter == null )
        {
            gameObject.SetActive( false );
            return;
        }

        var worldPos    = TargetCharacter.transform.position + Vector3.up * Constants.HP_GAUGE_WORLD_OFFSET_Y;
        var worldCamera = Camera.main;
        var screenPos   = RectTransformUtility.WorldToScreenPoint( worldCamera, worldPos );
        RectTransformUtility.ScreenPointToLocalPointInRectangle( _btlUiRectTransform, screenPos, _btlUiCamera, out var pos );
        _selfRectTransform.localPosition = pos;

        var status = TargetCharacter.GetStatusRef;
        float curRatio = ( status.MaxHP <= 0 ) ? 0f : ( float ) status.CurHP / status.MaxHP;

        UpdatePredictedDamageDisplay( curRatio, status.MaxHP );
    }

    /// <summary>
    /// 予測ダメージが設定されている場合、Fill自体は減少後のHP位置まで静的に縮め、
    /// その手前の「減少分」の範囲にFillと同色の別Imageを重ねてアルファ値を点滅させます。
    /// (伸縮ではなく、フェードで減少量を表現するため)
    /// </summary>
    private void UpdatePredictedDamageDisplay( float curRatio, int maxHp )
    {
        if( _predictedDamageAmount <= 0 || maxHp <= 0 )
        {
            _fillImage.fillAmount = curRatio;
            _predictedDamageImage.gameObject.SetActive( false );
            _defeatIconImage.gameObject.SetActive( false );
            return;
        }

        float lossRatio      = Mathf.Clamp01( ( float ) _predictedDamageAmount / maxHp );
        float retractedRatio = Mathf.Max( 0f, curRatio - lossRatio );

        _fillImage.fillAmount = retractedRatio;

        var rect = ( RectTransform ) _predictedDamageImage.transform;
        rect.anchorMin = new Vector2( retractedRatio, 0f );
        rect.anchorMax = new Vector2( curRatio, 1f );

        var color = _fillImage.color;
        color.a = Mathf.PingPong( Time.time * Constants.HP_GAUGE_PREDICTED_DAMAGE_BLINK_SPEED, 1f );
        _predictedDamageImage.color = color;

        _predictedDamageImage.gameObject.SetActive( true );

        // 予測ダメージで対象のHPが0以下になる(倒せる)場合は、撃破確定アイコンを表示する
        bool isLethal = TargetCharacter.GetStatusRef.CurHP <= _predictedDamageAmount;
        _defeatIconImage.gameObject.SetActive( isLethal );
    }

    private void CreateBackgroundImage()
    {
        var go = new GameObject( "Background", typeof( RectTransform ), typeof( Image ) );
        go.transform.SetParent( transform, false );

        var rect = ( RectTransform ) go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        int w = Mathf.RoundToInt( Constants.HP_GAUGE_WIDTH );
        int h = Mathf.RoundToInt( Constants.HP_GAUGE_HEIGHT );

        var image = go.GetComponent<Image>();
        image.sprite         = CreateRoundedRectSprite( w, h, h * Constants.HP_GAUGE_CORNER_RADIUS_RATIO, false );
        image.color          = Color.black;
        image.raycastTarget  = false;
    }

    private Image CreateFillImage()
    {
        var go = new GameObject( "Fill", typeof( RectTransform ), typeof( Image ) );
        go.transform.SetParent( transform, false );

        int bgW = Mathf.RoundToInt( Constants.HP_GAUGE_WIDTH );
        int bgH = Mathf.RoundToInt( Constants.HP_GAUGE_HEIGHT );

        // 下地よりひと回り小さいサイズ(HP_GAUGE_FILL_SIZE_RATIO倍)で中央に配置する。
        // ゲージの高さは元々小さいため、比率通りの余白を四捨五入すると縦方向の差が0pxに潰れてしまう場合がある。
        // そのため、縦横それぞれ最低1pxの余白を保証した上で、その実ピクセル比率をそのままRectTransformのアンカーにも反映する
        // (テクスチャの見た目とRectTransformの実表示サイズを一致させ、引き伸ばしによる歪みを防ぐ)
        int marginW = Mathf.Max( 1, Mathf.RoundToInt( bgW * ( 1f - Constants.HP_GAUGE_FILL_SIZE_RATIO ) * 0.5f ) );
        int marginH = Mathf.Max( 1, Mathf.RoundToInt( bgH * ( 1f - Constants.HP_GAUGE_FILL_SIZE_RATIO ) * 0.5f ) );
        int w = Mathf.Max( 1, bgW - marginW * 2 );
        int h = Mathf.Max( 1, bgH - marginH * 2 );

        float insetX = ( float ) marginW / bgW;
        float insetY = ( float ) marginH / bgH;
        var rect = ( RectTransform ) go.transform;
        rect.anchorMin = new Vector2( insetX, insetY );
        rect.anchorMax = new Vector2( 1f - insetX, 1f - insetY );
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.sprite         = CreateRoundedRectSprite( w, h, h * Constants.HP_GAUGE_CORNER_RADIUS_RATIO, true );
        image.type           = Image.Type.Filled;
        image.fillMethod     = Image.FillMethod.Horizontal;
        image.fillOrigin     = ( int ) Image.OriginHorizontal.Left;
        image.raycastTarget  = false;
        return image;
    }

    /// <summary>
    /// 予測ダメージ表示用のImageを、Fillの子として生成します。
    /// 角丸・グラデーションは付けず単色とし(狭い範囲に引き伸ばすと角丸が歪むため)、
    /// アンカーと色のアルファ値はUpdatePredictedDamageDisplay()で毎フレーム更新します。
    /// </summary>
    private Image CreatePredictedDamageImage()
    {
        var go = new GameObject( "PredictedDamage", typeof( RectTransform ), typeof( Image ) );
        go.transform.SetParent( _fillImage.transform, false );

        var rect = ( RectTransform ) go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.raycastTarget  = false;
        image.gameObject.SetActive( false );
        return image;
    }

    /// <summary>
    /// 撃破確定アイコンのImageを、ゲージ本体(transform)の子として生成します。
    /// アンカーをゲージ左端の中央1点に固定し、pivotを中心に取ることで、
    /// Fillのfillamountに関わらず常にゲージ左端に固定サイズのバッジとして表示されます。
    /// </summary>
    private Image CreateDefeatIconImage()
    {
        var go = new GameObject( "DefeatIcon", typeof( RectTransform ), typeof( Image ) );
        go.transform.SetParent( transform, false );

        var rect = ( RectTransform ) go.transform;
        rect.anchorMin         = new Vector2( 0f, 0.5f );
        rect.anchorMax         = new Vector2( 0f, 0.5f );
        rect.pivot             = new Vector2( 0.5f, 0.5f );
        rect.sizeDelta         = new Vector2( Constants.HP_GAUGE_DEFEAT_ICON_SIZE, Constants.HP_GAUGE_DEFEAT_ICON_SIZE );
        rect.anchoredPosition  = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.sprite         = _defeatIconSprite;
        image.raycastTarget  = false;
        image.gameObject.SetActive( false );
        return image;
    }

    /// <summary>
    /// 角丸矩形のアルファマスクを持つスプライトをランタイム生成します。
    /// gradientがtrueの場合、上を明るく下を暗くしたわずかなグラデーションもRGBに焼き込みます
    /// (Image.colorで色を乗算した際に、色付きのグラデーションとして見えるようにするため)。
    /// Image.Type.FilledはSpriteが未設定だとfillAmountを無視して全面描画されてしまうため必須。
    /// </summary>
    private static Sprite CreateRoundedRectSprite( int width, int height, float radius, bool gradient )
    {
        width  = Mathf.Max( width,  1 );
        height = Mathf.Max( height, 1 );

        var texture = new Texture2D( width, height, TextureFormat.RGBA32, false );
        texture.wrapMode   = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var pixels = new Color32[width * height];
        for( int y = 0; y < height; ++y )
        {
            float t         = ( height <= 1 ) ? 1f : ( float ) y / ( height - 1 ); // 0=下端、1=上端
            float luminance = gradient ? Mathf.Lerp( 0.72f, 1f, t ) : 1f;
            byte  v         = ( byte ) Mathf.RoundToInt( luminance * 255f );

            for( int x = 0; x < width; ++x )
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                // 角丸矩形の符号付き距離場(コア矩形上の最近傍点との距離)を利用したアンチエイリアス付きマスク
                float cx   = Mathf.Clamp( px, radius, width  - radius );
                float cy   = Mathf.Clamp( py, radius, height - radius );
                float dist = Mathf.Sqrt( ( px - cx ) * ( px - cx ) + ( py - cy ) * ( py - cy ) );
                float alpha = Mathf.Clamp01( radius - dist + 0.5f );

                pixels[y * width + x] = new Color32( v, v, v, ( byte ) Mathf.RoundToInt( alpha * 255f ) );
            }
        }

        texture.SetPixels32( pixels );
        texture.Apply();

        return Sprite.Create( texture, new Rect( 0f, 0f, width, height ), new Vector2( 0.5f, 0.5f ) );
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="rect">BattleUISystemのRectTransform</param>
    /// <param name="uiCamera">BattleUISystemに用いるUI用カメラ</param>
    public void Init( RectTransform rect, Camera uiCamera )
    {
        _btlUiRectTransform = rect;
        _btlUiCamera        = uiCamera;
    }

    /// <summary>
    /// 対象キャラクターを設定し、キャラクタータグに応じたゲージ色を適用した上で表示します
    /// </summary>
    /// <param name="chara">表示対象のキャラクター</param>
    public void ShowFor( Character chara )
    {
        EnsureBuilt();

        TargetCharacter   = chara;
        _fillImage.color  = GetGaugeColor( chara.GetStatusRef.characterTag );
        gameObject.SetActive( true );
    }

    /// <summary>
    /// HPゲージを明示的に非表示にします
    /// </summary>
    public void Hide()
    {
        TargetCharacter = null;
        gameObject.SetActive( false );
    }

    /// <summary>
    /// 予測ダメージ分の点滅表示を設定します。攻撃対象選択中、対象キャラクターに対して呼び出してください。
    /// </summary>
    /// <param name="amount">予測ダメージ量(HP減少量)。0以下の場合は点滅を行いません</param>
    public void SetPredictedDamage( int amount )
    {
        _predictedDamageAmount = Mathf.Max( 0, amount );
    }

    /// <summary>
    /// 予測ダメージ分の点滅表示を解除します
    /// </summary>
    public void ClearPredictedDamage()
    {
        _predictedDamageAmount = 0;
    }

    /// <summary>
    /// キャラクタータグに応じたゲージの色を取得します
    /// </summary>
    private static Color GetGaugeColor( CHARACTER_TAG tag )
    {
        switch( tag )
        {
            case CHARACTER_TAG.PLAYER: return Constants.HP_GAUGE_COLOR_PLAYER;
            case CHARACTER_TAG.ENEMY:  return Constants.HP_GAUGE_COLOR_ENEMY;
            case CHARACTER_TAG.OTHER:  return Constants.HP_GAUGE_COLOR_OTHER;
            default:                   return Color.white;
        }
    }
}
