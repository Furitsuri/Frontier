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
    /// 横切りアニメーションを開始します
    /// </summary>
    public void StartAnim()
    {
        _animator.SetTrigger("Crossing");
    }

    public bool IsPlayingAnim()
    {
        var info = _animator.GetCurrentAnimatorStateInfo(0);
        if( 0f < info.normalizedTime && info.normalizedTime < 1f )
        {
            return true;
        }

        return false;
    }

    public void Toggle()
    {
        gameObject.SetActive(false);
        foreach( TextMeshProUGUI text in PhaseText )
        {
            text.gameObject.SetActive(false);
        }
    }
}