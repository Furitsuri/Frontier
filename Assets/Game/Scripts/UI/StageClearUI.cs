using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageClearUI : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// �g��A�j���[�V�������J�n���܂�
    /// </summary>
    public void StartAnim()
    {
        _animator.SetTrigger("Enlarge");
    }
}
