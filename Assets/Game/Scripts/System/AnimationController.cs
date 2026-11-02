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
        // 各アニメーションがAnimatorに存在するか(初回参照時に求める。nullの場合は未取得)
        bool[] _hasAnimations = null;
        // 存在しない旨の警告を出力済みのアニメーション(同じ警告を繰り返し出力しないため)
        readonly HashSet<AnimDatas.AnimeConditionsTag> _warnedMissingTags = new HashSet<AnimDatas.AnimeConditionsTag>();

        public void Regist( Animator animator )
        {
            _readOnlyAnimator = new ReadOnlyReference<Animator>( animator );
            _hasAnimations    = null;
            _warnedMissingTags.Clear();
        }

        /// <summary>
        /// 指定のアニメーションがAnimatorに存在するかを判定します。
        /// MEMO : Animatorのパラメータ名とステート名が一致している(例 : Trigger「SingleAttack」→ステート「SingleAttack」)ことを前提に、パラメータの有無で判定します
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        /// <returns>存在するか否か</returns>
        public bool HasAnimation( AnimDatas.AnimeConditionsTag animTag )
        {
            if( animTag <= AnimDatas.AnimeConditionsTag.NONE || AnimDatas.AnimeConditionsTag.NUM <= animTag ) { return false; }

            if( null == _hasAnimations )
            {
                var parameters = _readOnlyAnimator.Value.parameters;
                // Animatorの初期化前などでパラメータが取得出来ない場合は、判定出来ないため存在するものとして扱う(結果は保持せず、次回再度取得する)
                if( parameters.Length <= 0 ) { return true; }

                _hasAnimations = new bool[( int ) AnimDatas.AnimeConditionsTag.NUM];
                foreach( var parameter in parameters )
                {
                    int index = AnimDatas.AnimNameHashList.IndexOf( parameter.nameHash );
                    if( 0 <= index ) { _hasAnimations[index] = true; }
                }
            }

            return _hasAnimations[( int ) animTag];
        }

        /// <summary>
        /// 実際に用いるアニメーションのタグを求めます。
        /// 指定のアニメーションがAnimatorに存在しない場合は警告を出力し、代わりのアニメーション(AnimDatas.FALLBACK_TAGS)を返します。
        /// 代わりのアニメーションも存在しない場合はNONEを返します
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        /// <returns>実際に用いるアニメーションのタグ</returns>
        private AnimDatas.AnimeConditionsTag ResolveAnimTag( AnimDatas.AnimeConditionsTag animTag )
        {
            if( animTag <= AnimDatas.AnimeConditionsTag.NONE || AnimDatas.AnimeConditionsTag.NUM <= animTag ) { return AnimDatas.AnimeConditionsTag.NONE; }
            if( HasAnimation( animTag ) ) { return animTag; }

            var fallbackTag     = AnimDatas.FALLBACK_TAGS[( int ) animTag];
            bool hasFallback    = HasAnimation( fallbackTag );

            if( _warnedMissingTags.Add( animTag ) )
            {
                var animator        = _readOnlyAnimator.Value;
                string missingName  = AnimDatas.ANIME_CONDITIONS_NAMES[( int ) animTag];
                string substitution = hasFallback ? $"{AnimDatas.ANIME_CONDITIONS_NAMES[( int ) fallbackTag]} で代用します" : "再生せず、すぐに終了したものとして扱います";
                Debug.LogWarning( $"[AnimationController] {animator.runtimeAnimatorController?.name} ({animator.gameObject.name}) に {missingName} のアニメーションが存在しないため、{substitution}。" );
            }

            return hasFallback ? fallbackTag : AnimDatas.AnimeConditionsTag.NONE;
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
            var resolvedTag = ResolveAnimTag( animTag );
            if( resolvedTag == AnimDatas.AnimeConditionsTag.NONE ) { return; }

            _readOnlyAnimator.Value.SetTrigger( AnimDatas.AnimNameHashList[( int ) resolvedTag] );
        }

        /// <summary>
        /// アニメーションを再生します
        /// </summary>
        /// <param name="animTag">アニメーションタグ</param>
        /// <param name="b">トリガーアニメーションに対して使用</param>
        public void SetAnimator( AnimDatas.AnimeConditionsTag animTag, bool b )
        {
            var resolvedTag = ResolveAnimTag( animTag );
            if( resolvedTag == AnimDatas.AnimeConditionsTag.NONE ) { return; }

            _readOnlyAnimator.Value.SetBool( AnimDatas.AnimNameHashList[( int ) resolvedTag], b );
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
            length = 0f;
            var resolvedTag = ResolveAnimTag( animTag );
            if( resolvedTag == AnimDatas.AnimeConditionsTag.NONE ) { return false; }

            return AnimStateLengthTable.TryGetLength( _readOnlyAnimator.Value.runtimeAnimatorController, resolvedTag, out length );
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
            time = 0f;
            var resolvedTag = ResolveAnimTag( animTag );
            if( resolvedTag == AnimDatas.AnimeConditionsTag.NONE ) { return false; }

            return AnimStateLengthTable.TryGetEventTime( _readOnlyAnimator.Value.runtimeAnimatorController, resolvedTag, functionName, out time );
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
            // 指定のアニメーションも代わりのアニメーションも存在しない場合は、現在のアニメーションを用いる
            animTag = ResolveAnimTag( animTag );

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
            // 指定のアニメーションも代わりのアニメーションも存在しない場合は、再生後すぐに終了したものとして扱う
            animTag = ResolveAnimTag( animTag );
            if( animTag == AnimDatas.AnimeConditionsTag.NONE ) { return false; }

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
            // 指定のアニメーションも代わりのアニメーションも存在しない場合は、再生後すぐに終了したものとして扱う
            animTag = ResolveAnimTag( animTag );
            if( animTag == AnimDatas.AnimeConditionsTag.NONE ) { return true; }

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