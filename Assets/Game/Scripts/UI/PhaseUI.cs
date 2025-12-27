using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PhaseUI : UiMonoBehaviour
{
    private Animator _animator;
    public TextMeshProUGUI[] PhaseText;

    /// <summary>
    /// 横切りアニメーションを開始します
    /// </summary>
    public void StartAnim()
    {
        _animator.SetTrigger( "Crossing" );
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

    /// <summary>
    /// ゲームオブジェクト及び下のレイヤーを非有効化にします
    /// 参照数は0ですが、アニメーションのイベントフラグから呼び出されています
    /// </summary>
    public void Toggle()
    {
        gameObject.SetActive( false );
        foreach( TextMeshProUGUI text in PhaseText )
        {
            text.gameObject.SetActive( false );
        }
    }

    public override void Setup()
    {
        base.Setup();

        LazyInject.GetOrCreate( ref _animator, () => GetComponent<Animator>() );
    }
}