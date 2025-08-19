using Frontier.Battle;
using Frontier.Combat;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using Zenject;
using static UnityEngine.EventSystems.StandaloneInputModule;

namespace Frontier.Entities
{
    public class CharacterRangedAttackAnimation : ICharacterCombatAnimation
    {
        private Character _character;
        private BattleRoutineController _btlRtnCtrl = null;
        private ReadOnlyCollection<AnimDatas.AnimeConditionsTag> AttackAnimTags;

        [Inject]
        void Construct( BattleRoutineController btlRtnCtrl )
        {
            _btlRtnCtrl = btlRtnCtrl;
        }

        public void Init( Character character, AnimDatas.AnimeConditionsTag[] consitionTags )
        {
            _character      = character;
            AttackAnimTags  = Array.AsReadOnly(consitionTags);
        }

        /// <summary>
        /// 遠隔攻撃シーケンスを開始します
        /// </summary>
        public void StartAttack()
        {
            _character.IsAttacked       = false;
            _character.AtkRemainingNum  = _character.skillModifiedParam.AtkNum - 1;   // 攻撃回数を1消費
            var attackAnimtag           = AttackAnimTags[_character.AtkRemainingNum];

            _character.AnimCtrl.SetAnimator(attackAnimtag);
        }

        /// <summary>
        /// 遠隔攻撃時の流れを更新します
        /// </summary>
        /// <param name="departure">遠隔攻撃の開始地点</param>
        /// <param name="destination">遠隔攻撃の終了地点</param>
        /// <returns>終了判定</returns>
        public bool UpdateAttack(in Vector3 departure, in Vector3 destination)
        {
            var bullet = _character.GetBullet();
            if (bullet == null) return false;

            // 遠隔攻撃は特定のフレームでカメラ対象とパラメータを変更する
            if (_character.IsTransitNextPhaseCamera())
            {
                _btlRtnCtrl.GetCameraController().TransitNextPhaseCameraParam(null, bullet.transform);
            }
            // 攻撃終了した場合はWaitに切り替え
            if (_character.IsEndAttackAnimSequence())
            {
                _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.WAIT);
            }

            // 対戦相手が攻撃を被弾済み、かつ、Wait状態に切り替え済みの場合に終了
            return _character.IsAttacked && _character.AnimCtrl.IsPlayingAnimationOnConditionTag(AnimDatas.AnimeConditionsTag.WAIT);
        }
    }
}
