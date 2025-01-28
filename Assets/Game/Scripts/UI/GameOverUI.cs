using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 落下アニメーションを開始します
    /// </summary>
    public void StartAnim()
    {
        _animator.SetTrigger("Fall");
    }
}
