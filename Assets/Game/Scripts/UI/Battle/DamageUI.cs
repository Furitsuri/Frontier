using TMPro;
using UnityEngine;

public class DamageUI : UiMonoBehaviour
{
    private RectTransform _btlUiRectTransform;
    private Camera _btlUiCamera;
    private float _remainingTime = -1f;
    public TextMeshProUGUI damageText;

    [Header( "出現演出" )]
    [Tooltip( "文字を上の桁(左)から順に、下からせり上がらせて出現させるアニメーター。移動量(position.from/to)・カーブはこちらで設定します" )]
    [SerializeField] private TextMeshProGeometryAnimator _revealAnimator;
    [Tooltip( "表示文字列全体が出現しきるまでの秒数。1文字あたりの出現時間は、この秒数を文字数で割った値になります" )]
    [SerializeField] private float _revealDuration = 0.4f;

    [Header( "表示枠" )]
    [Tooltip( "文字列が表示枠を超える場合に縮小する対象(damageTextを持つ子)。縮小はこのRectTransformのPivotを基準に行うため、Pivotは数字の足元(マスクの境目)に合わせてください" )]
    [SerializeField] private RectTransform _fitTarget;
    [Tooltip( "表示枠の幅の基準となる文字列。この文字列を等倍で表示した幅を枠とし、それより幅の広い文字列は枠に収まるよう全体を縮小します(狭い文字列は拡大しません)" )]
    [SerializeField] private string _fitReferenceText = "999";

    private bool _isFitPending = false;

    public Transform CharacterTransform { get; set; }

    void Update()
    {
        // 表示指示の時点で親階層が非アクティブだった場合、TMPが文字幅を測れないため、ここで改めて枠に収める
        if( _isFitPending && damageText.isActiveAndEnabled )
        {
            ApplyFitScale();
        }

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
        _isFitPending = true;
        if( damageText.isActiveAndEnabled )
        {
            ApplyFitScale();
        }
        PlayReveal();
    }

    /// <summary>
    /// 表示文字列の幅が表示枠(_fitReferenceTextを等倍で表示した幅)を超える場合、枠に収まるよう _fitTarget 全体を縮小します。
    /// フォントサイズではなく子ごと縮小することで、出現演出の移動量やマスクの境目との位置関係も同じ比率で縮み、
    /// 文字数に関わらず出現の見え方を保ったまま小さくできます。
    /// </summary>
    private void ApplyFitScale()
    {
        _isFitPending = false;
        if( _fitTarget == null ) { return; }

        float frameWidth    = damageText.GetPreferredValues( _fitReferenceText ).x;
        float textWidth     = damageText.GetPreferredValues( damageText.text ).x;
        float scale         = ( 0f < frameWidth && frameWidth < textWidth ) ? frameWidth / textWidth : 1f;

        _fitTarget.localScale = new Vector3( scale, scale, 1f );
    }

    /// <summary>
    /// 表示文字列を上の桁(左)から順にせり上がらせる出現演出を、最初から再生します。
    /// 文字数に関わらず全体が _revealDuration 秒で出現しきるよう、1文字あたりの出現時間と
    /// 次の文字が動き出すまでの間隔を、どちらも「全体の秒数 ÷ 文字数」に設定します。
    /// </summary>
    private void PlayReveal()
    {
        if( _revealAnimator == null ) { return; }

        // MEMO : 親階層が非アクティブの間はTMPのメッシュ(textInfo)が生成されないため、文字数は文字列から求める。
        //        リッチテキストのタグを含めると、タグの文字数まで数えてしまう点に注意
        int charCount = damageText.text?.Length ?? 0;
        if( charCount <= 0 ) { return; }

        // アニメーターの経過時間には再生速度(speed)が掛かるため、実時間の秒数に換算して設定する
        var position        = _revealAnimator.animationData.position;
        float speed         = Mathf.Max( _revealAnimator.animationData.speed, Mathf.Epsilon );
        float perCharTime   = _revealDuration * speed / charCount;
        position.delay      = 0f;
        position.wave       = perCharTime;
        position.time       = perCharTime;

        // 非アクティブの場合は、アクティブになった時点でアニメーター自身が(OnEnableで)再生する
        if( _revealAnimator.isActiveAndEnabled )
        {
            damageText.ForceMeshUpdate( true );
            _revealAnimator.Play();
        }
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
