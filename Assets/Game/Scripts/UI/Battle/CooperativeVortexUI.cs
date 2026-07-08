using UnityEngine;

/// <summary>
/// 連携演出において、キャラクターの画面上の位置(2Dカメラ座標)に渦巻きエフェクトを表示するUIです。
/// Play() で表示を開始し、指定時間をかけて縮小しながら自動的に非表示になります。
/// </summary>
public class CooperativeVortexUI : UiMonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 280f;   // 縮小中の回転速度(度/秒)

    private RectTransform _btlUiRectTransform;
    private Camera _btlUiCamera;
    private RectTransform _selfRectTransform;
    private float _duration = -1f;
    private float _elapsed;
    private float _initialScale = 1f;
    public Transform CharacterTransform { get; set; }

    void Update()
    {
        if( CharacterTransform == null ) { return; }

        var worldCamera = Camera.main;
        var screenPos   = RectTransformUtility.WorldToScreenPoint( worldCamera, CharacterTransform.position );
        RectTransformUtility.ScreenPointToLocalPointInRectangle( _btlUiRectTransform, screenPos, _btlUiCamera, out var pos );
        _selfRectTransform.localPosition = pos;

        if( _duration > 0f )
        {
            _elapsed += Time.deltaTime;
            float rate = Mathf.Clamp01( _elapsed / _duration );
            _selfRectTransform.localScale = Vector3.one * ( _initialScale * ( 1f - rate ) );
            _selfRectTransform.Rotate( 0f, 0f, _rotationSpeed * Time.deltaTime );

            if( rate >= 1f ) { Hide(); }
        }
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
        _selfRectTransform  = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 渦巻きエフェクトを表示します。duration 秒かけて縮小しながら回転し、経過後に自動的に非表示になります。
    /// </summary>
    /// <param name="duration">縮小にかける秒数</param>
    /// <param name="initialScale">縮小開始時の拡大率(等倍=1)</param>
    public void Play( float duration, float initialScale = 1f )
    {
        _duration     = duration;
        _elapsed      = 0f;
        _initialScale = initialScale;
        _selfRectTransform.localScale    = Vector3.one * initialScale;
        _selfRectTransform.localRotation = Quaternion.identity;
        gameObject.SetActive( true );
    }

    /// <summary>
    /// 渦巻きエフェクトを明示的に非表示にします
    /// </summary>
    public void Hide()
    {
        _duration = -1f;
        gameObject.SetActive( false );
    }
}
