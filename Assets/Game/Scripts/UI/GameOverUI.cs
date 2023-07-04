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
    /// �����A�j���[�V�������J�n���܂�
    /// </summary>
    public void StartAnim()
    {
        _animator.SetTrigger("Fall");
    }
}
