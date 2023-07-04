using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PhaseUI : MonoBehaviour
{
    private Animator _animator;
    public TextMeshProUGUI[] PhaseText;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// ���؂�A�j���[�V�������J�n���܂�
    /// </summary>
    public void StartAnim()
    {
        _animator.SetTrigger("Crossing");
    }

    /// <summary>
    /// �A�j���[�V�������Đ�����Ă��邩�ǂ����𔻒肵�܂�
    /// </summary>
    /// <returns>�Đ�����Ă��邩�ۂ�</returns>
    public bool IsPlayingAnim()
    {
        var info = _animator.GetCurrentAnimatorStateInfo(0);
        if( 0f < info.normalizedTime && info.normalizedTime < 1f )
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// �Q�[���I�u�W�F�N�g�y�щ��̃��C���[���L�����ɂ��܂�
    /// �Q�Ɛ���0�ł����A�A�j���[�V�����̃C�x���g�t���O����Ăяo����Ă��܂�
    /// </summary>
    public void Toggle()
    {
        gameObject.SetActive(false);
        foreach (TextMeshProUGUI text in PhaseText)
        {
            text.gameObject.SetActive(false);
        }
    }
}