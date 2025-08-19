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
        /// ���u�U���V�[�P���X���J�n���܂�
        /// </summary>
        public void StartAttack()
        {
            _character.IsAttacked       = false;
            _character.AtkRemainingNum  = _character.skillModifiedParam.AtkNum - 1;   // �U���񐔂�1����
            var attackAnimtag           = AttackAnimTags[_character.AtkRemainingNum];

            _character.AnimCtrl.SetAnimator(attackAnimtag);
        }

        /// <summary>
        /// ���u�U�����̗�����X�V���܂�
        /// </summary>
        /// <param name="departure">���u�U���̊J�n�n�_</param>
        /// <param name="destination">���u�U���̏I���n�_</param>
        /// <returns>�I������</returns>
        public bool UpdateAttack(in Vector3 departure, in Vector3 destination)
        {
            var bullet = _character.GetBullet();
            if (bullet == null) return false;

            // ���u�U���͓���̃t���[���ŃJ�����Ώۂƃp�����[�^��ύX����
            if (_character.IsTransitNextPhaseCamera())
            {
                _btlRtnCtrl.GetCameraController().TransitNextPhaseCameraParam(null, bullet.transform);
            }
            // �U���I�������ꍇ��Wait�ɐ؂�ւ�
            if (_character.IsEndAttackAnimSequence())
            {
                _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.WAIT);
            }

            // �ΐ푊�肪�U�����e�ς݁A���AWait��Ԃɐ؂�ւ��ς݂̏ꍇ�ɏI��
            return _character.IsAttacked && _character.AnimCtrl.IsPlayingAnimationOnConditionTag(AnimDatas.AnimeConditionsTag.WAIT);
        }
    }
}
