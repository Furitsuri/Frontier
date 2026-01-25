using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// アニメーションまわりの制御を行います
    /// </summary>
    public class AnimationController
    {
        ReadOnlyReference<Animator> _readOnlyeAnimator = null;

        public void Regist( Animator animator )
        {
            _readOnlyeAnimator = new ReadOnlyReference<Animator>( animator );
        }

        /// <summary>
        /// キャラクターのタイムスケールを更新します
        /// </summary>
        /// <param name="timeScale">スケール値</param>
        public void UpdateTimeScale( float timeScale )
        {
            _readOnlyeAnimator.Value.speed = timeScale;
        }

        /// <summary>
        /// アニメーションを再生します
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        public void SetAnimator( AnimDatas.AnimeConditionsTag animTag )
        {
            _readOnlyeAnimator.Value.SetTrigger( AnimDatas.AnimNameHashList[( int ) animTag] );
        }

        /// <summary>
        /// アニメーションを再生します
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        /// <param name="b">トリガーアニメーションに対して使用</param>
        public void SetAnimator( AnimDatas.AnimeConditionsTag animTag, bool b )
        {
            _readOnlyeAnimator.Value.SetBool( AnimDatas.AnimNameHashList[( int ) animTag], b );
        }

        /// <summary>
        /// 現在再生中のアニメーションを再開します
        /// </summary>
        public void RestartAnimator()
        {
            _readOnlyeAnimator.Value.enabled = true;
        }

        /// <summary>
        /// 現在再生中のアニメーションを先頭にスナップします
        /// </summary>
        public void SnapToCurrentAnimationToStart( AnimDatas.AnimeConditionsTag animTag )
        {
            if( animTag == AnimDatas.AnimeConditionsTag.NONE )
            {
                int hash;
                if( _readOnlyeAnimator.Value.IsInTransition( 0 ) )
                {
                    // 遷移中なら遷移先
                    hash = _readOnlyeAnimator.Value.GetNextAnimatorStateInfo( 0 ).fullPathHash;
                }
                else
                {
                    // 通常時なら現在
                    hash = _readOnlyeAnimator.Value.GetCurrentAnimatorStateInfo( 0 ).fullPathHash;
                }

                _readOnlyeAnimator.Value.Play( hash, 0, 0f );
            }
            else
            {
                _readOnlyeAnimator.Value.Play( AnimDatas.ANIME_CONDITIONS_NAMES[( int ) animTag], 0, 0f );
            }

            // 強制反映
            _readOnlyeAnimator.Value.Update( Time.deltaTime );

            _readOnlyeAnimator.Value.enabled = false;
        }

        /// <summary>
        /// 指定のアニメーションを再生中かを判定します
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        /// <returns>true : 再生中, false : 再生していない</returns>
        public bool IsPlayingAnimationOnConditionTag( AnimDatas.AnimeConditionsTag animTag )
        {
            AnimatorStateInfo stateInfo = _readOnlyeAnimator.Value.GetCurrentAnimatorStateInfo( 0 );

            // MEMO : animator側でHasExitTime(終了時間あり)をONにしている場合、終了時間を1.0に設定する必要があることに注意
            if( stateInfo.IsName( AnimDatas.ANIME_CONDITIONS_NAMES[( int ) animTag] ) && stateInfo.normalizedTime < 1f )
            {
                return true;
            }

            return false;
        }

        public bool IsEndCurrentAnimation()
        {
            AnimatorStateInfo stateInfo = _readOnlyeAnimator.Value.GetCurrentAnimatorStateInfo( 0 );

            // MEMO : animator側でHasExitTime(終了時間あり)をONにしている場合、終了時間を1.0に設定する必要があることに注意
            if( 1f <= stateInfo.normalizedTime )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 指定のアニメーションが終了したかを判定します
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        /// <returns>true : 終了, false : 未終了</returns>
        public bool IsEndAnimationOnConditionTag( AnimDatas.AnimeConditionsTag animTag )
        {
            AnimatorStateInfo stateInfo = _readOnlyeAnimator.Value.GetCurrentAnimatorStateInfo( 0 );

            // MEMO : animator側でHasExitTime(終了時間あり)をONにしている場合、終了時間を1.0に設定する必要があることに注意
            if( stateInfo.IsName( AnimDatas.ANIME_CONDITIONS_NAMES[( int ) animTag] ) && 1f <= stateInfo.normalizedTime )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 現在のアニメーションが終了したかを判定します
        /// </summary>
        /// <returns>true : 終了, false : 未終了</returns>
        public bool IsEndAnimationOnStateName( string stateName )
        {
            AnimatorStateInfo stateInfo = _readOnlyeAnimator.Value.GetCurrentAnimatorStateInfo( 0 );

            // MEMO : animator側でHasExitTime(終了時間あり)をONにしている場合、終了時間を1.0に設定する必要があることに注意
            if( stateInfo.IsName( stateName ) && 1f <= stateInfo.normalizedTime )
            {
                return true;
            }

            return false;
        }
    }
}