using TMPro;
using UnityEngine;

public class DamageUI : UiMonoBehaviour
{
    private RectTransform _btlUiRectTransform;
    private Camera _btlUiCamera;
    private float _remainingTime = -1f;
    public TextMeshProUGUI damageText;
    public Transform CharacterTransform { get; set; }

    void Update()
    {
        // 対象キャラクターが破棄され CharacterTransform が null になった後も、
        // 自動非表示のカウントダウン(_remainingTime)は進める必要があるため、
        // 位置追従処理とは独立して進行させる
        if( CharacterTransform != null )
        {
            var pos         = Vector2.zero;
            var worldCamera = Camera.main;
            var screenPos   = RectTransformUtility.WorldToScreenPoint( worldCamera, CharacterTransform.position );
            RectTransformUtility.ScreenPointToLocalPointInRectangle( _btlUiRectTransform, screenPos, _btlUiCamera, out pos );
            GetComponent<RectTransform>().localPosition = pos;
        }

        if( _remainingTime > 0f )
        {
            _remainingTime -= Time.deltaTime;
            if( _remainingTime <= 0f )
            {
                gameObject.SetActive( false );
            }
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
    }

    /// <summary>
    /// ダメージUIを表示します。duration が 0 以上の場合、指定秒数後に自動で非表示にします。
    /// duration が負の値の場合は自動非表示を行わず、Hide() による明示的な非表示が必要です。
    /// </summary>
    /// <param name="duration">自動非表示までの秒数。負の値で無効。</param>
    public void ShowWith( float duration )
    {
        _remainingTime = duration;
        gameObject.SetActive( true );
    }

    /// <summary>
    /// ダメージUIを明示的に非表示にします
    /// </summary>
    public void Hide()
    {
        _remainingTime = -1f;
        gameObject.SetActive( false );
    }
}
