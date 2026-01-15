using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageClearUI : UiMonoBehaviour
{
    private Animator _animator;

    /// <summary>
    /// 拡大アニメーションを開始します
    /// </summary>
    public void StartAnim()
    {
        _animator.SetTrigger("Enlarge");
    }

    public override void Setup()
    {
        base.Setup();

        LazyInject.GetOrCreate(ref _animator, () => GetComponent<Animator>());
    }
}
