using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// �A�j���[�V�����܂��̐�����s���܂�
    /// </summary>
    public class AnimationController
    {
        Animator _animator;

        public AnimationController() { }

        public void Init( Animator animator )
        {
            _animator = animator;
        }

        /// <summary>
        /// �L�����N�^�[�̃^�C���X�P�[�����X�V���܂�
        /// </summary>
        /// <param name="timeScale">�X�P�[���l</param>
        public void UpdateTimeScale(float timeScale)
        {
            _animator.speed = timeScale;
        }

        /// <summary>
        /// �A�j���[�V�������Đ����܂�
        /// </summary>
        /// <param name="animTag">�A�j���[�V�����^�O</param>
        public void SetAnimator(AnimDatas.AnimeConditionsTag animTag)
        {
            _animator.SetTrigger(AnimDatas.AnimNameHashList[(int)animTag]);
        }

        /// <summary>
        /// �A�j���[�V�������Đ����܂�
        /// </summary>
        /// <param name="animTag">�A�j���[�V�����^�O</param>
        /// <param name="b">�g���K�[�A�j���[�V�����ɑ΂��Ďg�p</param>
        public void SetAnimator(AnimDatas.AnimeConditionsTag animTag, bool b)
        {
            _animator.SetBool(AnimDatas.AnimNameHashList[(int)animTag], b);
        }

        /// <summary>
        /// �w��̃A�j���[�V�������Đ������𔻒肵�܂�
        /// </summary>
        /// <param name="animTag">�A�j���[�V�����^�O</param>
        /// <returns>true : �Đ���, false : �Đ����Ă��Ȃ�</returns>
        public bool IsPlayingAnimationOnConditionTag(AnimDatas.AnimeConditionsTag animTag)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            // MEMO : animator����HasExitTime(�I�����Ԃ���)��ON�ɂ��Ă���ꍇ�A�I�����Ԃ�1.0�ɐݒ肷��K�v�����邱�Ƃɒ���
            if (stateInfo.IsName(AnimDatas.ANIME_CONDITIONS_NAMES[(int)animTag]) && stateInfo.normalizedTime < 1f)
            {
                return true;
            }

            return false;
        }

        public bool IsEndCurrentAnimation()
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            // MEMO : animator����HasExitTime(�I�����Ԃ���)��ON�ɂ��Ă���ꍇ�A�I�����Ԃ�1.0�ɐݒ肷��K�v�����邱�Ƃɒ���
            if (1f <= stateInfo.normalizedTime)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// �w��̃A�j���[�V�������I���������𔻒肵�܂�
        /// </summary>
        /// <param name="animTag">�A�j���[�V�����^�O</param>
        /// <returns>true : �I��, false : ���I��</returns>
        public bool IsEndAnimationOnConditionTag(AnimDatas.AnimeConditionsTag animTag)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            // MEMO : animator����HasExitTime(�I�����Ԃ���)��ON�ɂ��Ă���ꍇ�A�I�����Ԃ�1.0�ɐݒ肷��K�v�����邱�Ƃɒ���
            if (stateInfo.IsName(AnimDatas.ANIME_CONDITIONS_NAMES[(int)animTag]) && 1f <= stateInfo.normalizedTime)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// ���݂̃A�j���[�V�������I���������𔻒肵�܂�
        /// </summary>
        /// <returns>true : �I��, false : ���I��</returns>
        public bool IsEndAnimationOnStateName(string stateName)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            // MEMO : animator����HasExitTime(�I�����Ԃ���)��ON�ɂ��Ă���ꍇ�A�I�����Ԃ�1.0�ɐݒ肷��K�v�����邱�Ƃɒ���
            if (stateInfo.IsName(stateName) && 1f <= stateInfo.normalizedTime)
            {
                return true;
            }

            return false;
        }
    }
}