using Frontier.Battle;
using TMPro;
using UnityEngine;

public class PhaseUI : UiMonoBehaviour
{
    private int _currentTurnTypeIndex;
    private Animator _animator;
    [SerializeField] private string[] _animNames;
    [SerializeField] private TextMeshProUGUI[] _phaseText;

    /// <summary>
    /// 横切りアニメーションを開始します
    /// </summary>
    public void StartAnim()
    {
        gameObject.SetActive( true );
        _animator.SetTrigger( _animNames[_currentTurnTypeIndex] );
    }

    /// <summary>
    /// gameObjectを有効化していないとアニメーション自体が再生されないため、
    /// SetActive( true )はStartAnimから呼び出しています。
    /// 参照数は0ですが、アニメーションのイベントフラグから呼び出されています
    /// </summary>
    public void Activate()
    {
        _phaseText[_currentTurnTypeIndex].gameObject.SetActive( true );
    }

    /// <summary>
    /// 参照数は0ですが、アニメーションのイベントフラグから呼び出されています
    /// </summary>
    public void Deactivate()
    {
        gameObject.SetActive( false );
        _phaseText[_currentTurnTypeIndex].gameObject.SetActive( false );
    }

    public void SetTurnType( TurnType turnType )
    {
        _currentTurnTypeIndex = ( int ) turnType;
    }

    /// <summary>
    /// アニメーションが再生されているかどうかを判定します
    /// </summary>
    /// <returns>再生されているか否か</returns>
    public bool IsPlayingAnim()
    {
        var info = _animator.GetCurrentAnimatorStateInfo( 0 );
        if( 0f < info.length && info.normalizedTime < 1f )
        {
            return true;
        }

        return false;
    }

    public override void Setup()
    {
        base.Setup();

        LazyInject.GetOrCreate( ref _animator, () => GetComponent<Animator>() );
    }
}