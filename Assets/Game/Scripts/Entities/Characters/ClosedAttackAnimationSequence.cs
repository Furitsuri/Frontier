using Frontier.Combat;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using UnityEngine;
using Frontier.Combat.Skill;

namespace Frontier.Entities
{
    public class ClosedAttackAnimationSequence : ICombatAnimationSequence
    {
        private CLOSED_ATTACK_PHASE _closingAttackPhase;
        private Character _character;
        private ReadOnlyCollection<AnimDatas.AnimeConditionsTag> AttackAnimTags;

        /// <summary>
        /// �U���A�j���[�V�����̏I�������Ԃ��܂�
        /// </summary>
        /// <returns>�U���A�j���[�V�������I�����Ă��邩</returns>
        private bool IsEndAttackAnimSequence()
        {
            return _character.AnimCtrl.IsEndAnimationOnStateName(AnimDatas.AtkEndStateName) ||                  // �Ō�̍U����State���͕K��AtkEndStateName�ň�v������
                (_character.GetOpponentChara().IsDeclaredDead && _character.AnimCtrl.IsEndCurrentAnimation());  // ������U�����ł��A�r���ő��肪���S���邱�Ƃ��m�񂳂��ꍇ�͍U�����I������
        }

        public void Init( Character character, AnimDatas.AnimeConditionsTag[] consitionTags)
        {
            _character          = character;
            AttackAnimTags      = Array.AsReadOnly(consitionTags);
            _closingAttackPhase = CLOSED_ATTACK_PHASE.NONE;
        }

        /// <summary>
        /// �ߐڍU���V�[�P���X���J�n���܂�
        /// </summary>
        public void StartSequence()
        {
            _character.IsAttacked   = false;
            _closingAttackPhase     = CLOSED_ATTACK_PHASE.CLOSINGE;
            _character.ResetElapsedTime();

            _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.MOVE, true);
        }

        /// <summary>
        /// �ߐڍU�����̗�����X�V���܂�
        /// </summary>
        /// <param name="departure">�ߐڍU���̊J�n�n�_</param>
        /// <param name="destination">�ߐڍU���̏I���n�_</param>
        /// <returns>�I������</returns>
        public bool UpdateSequence( in Vector3 departure, in Vector3 destination )
        {
            var attackAnimtag = AttackAnimTags[_character.Params.SkillModifiedParam.AtkNum - 1];

            if ( _character.GetBullet() != null ) return false;

            float t = 0f;
            bool isReservedParry = (0 <= _character.GetOpponentChara().GetUsingSkillSlotIndexById(ID.SKILL_PARRY));

            switch ( _closingAttackPhase )
            {
                case CLOSED_ATTACK_PHASE.CLOSINGE:
                    _character.ElapsedTime += DeltaTimeProvider.DeltaTime;
                    t = Mathf.Clamp01( _character.ElapsedTime / Constants.ATTACK_CLOSING_TIME );
                    t = Mathf.SmoothStep( 0f, 1f, t );
                    _character.gameObject.transform.position = Vector3.Lerp( departure, destination, t );
                    if ( 1.0f <= t )
                    {
                        _character.ResetElapsedTime();
                        _character.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.MOVE, false );
                        _character.AnimCtrl.SetAnimator( attackAnimtag );

                        _closingAttackPhase = CLOSED_ATTACK_PHASE.ATTACK;
                    }
                    break;

                case CLOSED_ATTACK_PHASE.ATTACK:
                    if ( IsEndAttackAnimSequence() )
                    {
                        _character.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.WAIT );

                        _closingAttackPhase = CLOSED_ATTACK_PHASE.DISTANCING;
                    }
                    break;
                case CLOSED_ATTACK_PHASE.DISTANCING:
                    // �U���O�̏ꏊ�ɖ߂�
                    _character.ElapsedTime += DeltaTimeProvider.DeltaTime;
                    t = Mathf.Clamp01( _character.ElapsedTime / Constants.ATTACK_DISTANCING_TIME );
                    t = Mathf.SmoothStep( 0f, 1f, t );
                    _character.gameObject.transform.position = Vector3.Lerp( destination, departure, t );
                    if ( 1.0f <= t )
                    {
                        _character.ResetElapsedTime();
                        _closingAttackPhase = CLOSED_ATTACK_PHASE.NONE;

                        return true;
                    }
                    break;
                default: break;
            }

            return false;
        }
    }
}