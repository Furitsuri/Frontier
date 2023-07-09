using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Player : Character
{
    override public void setAnimator(ANIME_TAG animTag)
    {
        _animator.SetTrigger(_animNames[(int)animTag]);
    }
    override public void setAnimator(ANIME_TAG animTag, bool b)
    {
        _animator.SetBool(_animNames[(int)animTag], b);
    }

    /// <summary>
    /// ���S�����B�Ǘ����X�g����폜���A�Q�[���I�u�W�F�N�g��j�����܂�
    /// ���[�V�����̃C�x���g�t���O����Ăяo���܂�
    /// </summary>
    public override void Die()
    {
        base.Die();

        BattleManager.Instance.RemovePlayerFromList(this);
    }
}