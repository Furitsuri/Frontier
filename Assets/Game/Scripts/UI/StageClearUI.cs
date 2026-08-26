using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageClearUI : UiMonoBehaviour
{
    private Animator _animator;
    private bool _isPlaying = false;

    /// <summary>
    /// 拡大アニメーションを開始します
    /// </summary>
    public void StartAnim()
    {
        _isPlaying = true;
        _animator.SetTrigger("Enlarge");
    }

    /// <summary>
    /// 拡大アニメーションを再生中かどうかを返します。
    /// StartAnim()呼び出し時にtrueとなり、Animatorが"None"ステートへ戻った時点でfalseになります。
    /// </summary>
    public bool IsPlayingAnim()
    {
        return _isPlaying;
    }

    void Update()
    {
        if( !_isPlaying ) { return; }

        // 遷移中は現在のステート名だけでは判定出来ないため、遷移が完了し、
        // かつ"None"ステートに戻ったタイミングでのみ再生終了とみなす
        if( !_animator.IsInTransition( 0 ) && _animator.GetCurrentAnimatorStateInfo( 0 ).IsName( "None" ) )
        {
            _isPlaying = false;
        }
    }

    public override void Setup()
    {
        base.Setup();

        LazyInject.GetOrCreate(ref _animator, () => GetComponent<Animator>());
    }
}
