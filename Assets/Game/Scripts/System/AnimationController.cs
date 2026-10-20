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
        ReadOnlyReference<Animator> _readOnlyAnimator = null;

        public void Regist( Animator animator )
        {
            _readOnlyAnimator = new ReadOnlyReference<Animator>( animator );
        }

        /// <summary>
        /// キャラクターのタイムスケールを更新します
        /// </summary>
        /// <param name="timeScale">スケール値</param>
        public void UpdateTimeScale( float timeScale )
        {
            _readOnlyAnimator.Value.speed = timeScale;
        }

        /// <summary>
        /// アニメーションを再生します
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        public void SetAnimator( AnimDatas.AnimeConditionsTag animTag )
        {
            _readOnlyAnimator.Value.SetTrigger( AnimDatas.AnimNameHashList[( int ) animTag] );
        }

        /// <summary>
        /// アニメーションを再生します
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        /// <param name="b">トリガーアニメーションに対して使用</param>
        public void SetAnimator( AnimDatas.AnimeConditionsTag animTag, bool b )
        {
            _readOnlyAnimator.Value.SetBool( AnimDatas.AnimNameHashList[( int ) animTag], b );
        }

        /// <summary>
        /// 指定のアニメーションの再生時間(秒)を取得します。
        /// 再生時間は AnimStateLengthTable にエディタ上で自動収集されたものを用います。
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        /// <param name="length">再生時間(秒)</param>
        /// <returns>取得出来た場合はtrue</returns>
        public bool TryGetAnimationLength( AnimDatas.AnimeConditionsTag animTag, out float length )
        {
            return AnimStateLengthTable.TryGetLength( _readOnlyAnimator.Value.runtimeAnimatorController, animTag, out length );
        }

        /// <summary>
        /// 指定のアニメーションの開始から、指定のアニメーションイベントまでの時間(秒)を取得します。
        /// 時間は AnimStateLengthTable にエディタ上で自動収集されたものを用います。
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        /// <param name="functionName">イベントの関数名。nameof( BattleAnimationEventReceiver.XXX ) の形で指定してください</param>
        /// <param name="time">イベントまでの時間(秒)</param>
        /// <returns>取得出来た場合はtrue(イベントが設定されていない場合はfalse)</returns>
        public bool TryGetEventTime( AnimDatas.AnimeConditionsTag animTag, string functionName, out float time )
        {
            return AnimStateLengthTable.TryGetEventTime( _readOnlyAnimator.Value.runtimeAnimatorController, animTag, functionName, out time );
        }

        /// <summary>
        /// 現在再生中のアニメーションを再開します
        /// </summary>
        public void RestartAnimator()
        {
            _readOnlyAnimator.Value.enabled = true;
        }

        /// <summary>
        /// 現在再生中のアニメーションを先頭にスナップします
        /// </summary>
        public void SnapToCurrentAnimationToStart( AnimDatas.AnimeConditionsTag animTag )
        {
            if( animTag == AnimDatas.AnimeConditionsTag.NONE )
            {
                int hash;
                if( _readOnlyAnimator.Value.IsInTransition( 0 ) )
                {
                    // 遷移中なら遷移先
                    hash = _readOnlyAnimator.Value.GetNextAnimatorStateInfo( 0 ).fullPathHash;
                }
                else
                {
                    // 通常時なら現在
                    hash = _readOnlyAnimator.Value.GetCurrentAnimatorStateInfo( 0 ).fullPathHash;
                }

                _readOnlyAnimator.Value.Play( hash, 0, 0f );
            }
            else
            {
                _readOnlyAnimator.Value.Play( AnimDatas.ANIME_CONDITIONS_NAMES[( int ) animTag], 0, 0f );
            }

            // 強制反映
            _readOnlyAnimator.Value.Update( Time.deltaTime );

            _readOnlyAnimator.Value.enabled = false;
        }

        /// <summary>
        /// 指定のアニメーションを再生中かを判定します
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        /// <returns>true : 再生中, false : 再生していない</returns>
        public bool IsPlayingAnimationOnConditionTag( AnimDatas.AnimeConditionsTag animTag )
        {
            AnimatorStateInfo stateInfo = _readOnlyAnimator.Value.GetCurrentAnimatorStateInfo( 0 );

            // MEMO : animator側でHasExitTime(終了時間あり)をONにしている場合、終了時間を1.0に設定する必要があることに注意
            if( stateInfo.IsName( AnimDatas.ANIME_CONDITIONS_NAMES[( int ) animTag] ) && stateInfo.normalizedTime < 1f )
            {
                return true;
            }

            return false;
        }

        public bool IsEndCurrentAnimation()
        {
            AnimatorStateInfo stateInfo = _readOnlyAnimator.Value.GetCurrentAnimatorStateInfo( 0 );

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
            AnimatorStateInfo stateInfo = _readOnlyAnimator.Value.GetCurrentAnimatorStateInfo( 0 );

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
            AnimatorStateInfo stateInfo = _readOnlyAnimator.Value.GetCurrentAnimatorStateInfo( 0 );

            // MEMO : animator側でHasExitTime(終了時間あり)をONにしている場合、終了時間を1.0に設定する必要があることに注意
            if( stateInfo.IsName( stateName ) && 1f <= stateInfo.normalizedTime )
            {
                return true;
            }

            return false;
        }
    }
}