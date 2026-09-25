using Frontier.Combat;
using System;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// 相手へ駆け寄らず、その場で近接攻撃モーションを再生するシーケンスです。
    /// ステージ上でそのまま攻撃を完結させる InPlaceAttackSequence で使用します。
    /// </summary>
    public class InPlaceClosedAttackAnimationSequence : ICombatAnimationSequence
    {
        private Character _character;
        private BattleAnimationEventReceiver _animReceiver;
        private ReadOnlyCollection<AnimDatas.AnimeConditionsTag> AttackAnimTags;
        private bool _isAttacking;

        /// <summary>
        /// 攻撃アニメーションの終了判定を返します
        /// </summary>
        /// <returns>攻撃アニメーションが終了しているか</returns>
        private bool IsEndAttackAnimSequence()
        {
            return _character.AnimCtrl.IsEndAnimationOnStateName( AnimDatas.AtkEndStateName ) ||                  // 最後の攻撃のState名は必ずAtkEndStateNameで一致させる
                ( _character.BattleLogic.GetOpponent().BattleLogic.IsDeclaredDead && _character.AnimCtrl.IsEndCurrentAnimation() );  // 複数回攻撃時でも、途中で相手が死亡することが確約される場合は攻撃を終了する
        }

        public void Init( Character character, AnimDatas.AnimeConditionsTag[] consitionTags )
        {
            _character      = character;
            _animReceiver   = character.BtlAnimReceiver;
            AttackAnimTags  = Array.AsReadOnly( consitionTags );
            _isAttacking    = false;
        }

        /// <summary>
        /// その場での近接攻撃シーケンスを開始します
        /// </summary>
        public void StartSequence()
        {
            _animReceiver.IsAttacked = false;
            _isAttacking             = true;

            _character.AnimCtrl.SetAnimator( AttackAnimTags[_character.BattleParams.SkillModifiedParam.AddAtkNum] );
        }

        /// <summary>
        /// その場での近接攻撃の流れを更新します
        /// </summary>
        /// <param name="departure">未使用(移動を伴わないため)</param>
        /// <param name="destination">未使用(移動を伴わないため)</param>
        /// <returns>終了判定</returns>
        public bool UpdateSequence( in Vector3 departure, in Vector3 destination )
        {
            if( _character.GetBullet() != null ) return false;
            if( !_isAttacking ) return false;

            if( IsEndAttackAnimSequence() )
            {
                _character.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.WAIT );
                _isAttacking = false;

                return true;
            }

            return false;
        }
    }
}
