using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverUI : UiMonoBehaviour
{
    private Animator _animator;

    /// <summary>
    /// 落下アニメーションを開始します
    /// </summary>
    public void StartAnim()
    {
        _animator.SetTrigger("Fall");
    }

    override public void Setup()
    {
        base.Setup();

        LazyInject.GetOrCreate(ref _animator, () => GetComponent<Animator>());
    }
}
