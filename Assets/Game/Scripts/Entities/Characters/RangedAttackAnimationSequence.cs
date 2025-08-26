using Frontier.Battle;
using Frontier.Combat;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    public class RangedAttackAnimationSequence : ICombatAnimationSequence
    {
        private Character _character;
        private BattleRoutineController _btlRtnCtrl = null;
        private ReadOnlyCollection<AnimDatas.AnimeConditionsTag> AttackAnimTags;

        [Inject]
        void Construct( BattleRoutineController btlRtnCtrl )
        {
            _btlRtnCtrl = btlRtnCtrl;
        }

        /// <summary>
        /// 攻撃アニメーションの終了判定を返します
        /// </summary>
        /// <returns>攻撃アニメーションが終了しているか</returns>
        private bool IsEndAttackAnimSequence()
        {
            return _character.AnimCtrl.IsEndAnimationOnStateName(AnimDatas.AtkEndStateName) ||                  // 最後の攻撃のState名は必ずAtkEndStateNameで一致させる
                (_character.GetOpponentChara().IsDeclaredDead && _character.AnimCtrl.IsEndCurrentAnimation());  // 複数回攻撃時でも、途中で相手が死亡することが確約される場合は攻撃を終了する
        }

        public void Init( Character character, AnimDatas.AnimeConditionsTag[] consitionTags )
        {
            _character      = character;
            AttackAnimTags  = Array.AsReadOnly(consitionTags);
        }

        /// <summary>
        /// 遠隔攻撃シーケンスを開始します
        /// </summary>
        public void StartSequence()
        {
            _character.IsAttacked       = false;
            _character.AtkRemainingNum  = _character.Params.SkillModifiedParam.AtkNum - 1;   // 攻撃回数を1消費
            var attackAnimtag           = AttackAnimTags[_character.AtkRemainingNum];

            _character.AnimCtrl.SetAnimator(attackAnimtag);
        }

        /// <summary>
        /// 遠隔攻撃時の流れを更新します
        /// </summary>
        /// <param name="departure">遠隔攻撃の開始地点</param>
        /// <param name="destination">遠隔攻撃の終了地点</param>
        /// <returns>終了判定</returns>
        public bool UpdateSequence(in Vector3 departure, in Vector3 destination)
        {
            var bullet = _character.GetBullet();
            if (bullet == null) return false;

            // 遠隔攻撃は特定のフレームでカメラ対象とパラメータを変更する
            if (_character.IsTransitNextPhaseCamera())
            {
                _btlRtnCtrl.GetCameraController().TransitNextPhaseCameraParam(null, bullet.transform);
            }
            // 攻撃終了した場合はWaitに切り替え
            if (IsEndAttackAnimSequence())
            {
                _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.WAIT);
            }

            // 対戦相手が攻撃を被弾済み、かつ、Wait状態に切り替え済みの場合に終了
            return _character.IsAttacked && _character.AnimCtrl.IsPlayingAnimationOnConditionTag(AnimDatas.AnimeConditionsTag.WAIT);
        }
    }
}
