using Frontier.Combat;
using System;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Frontier.Entities
{
    public class CharacterClosedAttackAnimation : ICharacterCombatAnimation
    {
        private CLOSED_ATTACK_PHASE _closingAttackPhase;
        private Character _character;
        private ReadOnlyCollection<AnimDatas.AnimeConditionsTag> AttackAnimTags;

        public void Init( Character character, AnimDatas.AnimeConditionsTag[] consitionTags)
        {
            _character          = character;
            AttackAnimTags      = Array.AsReadOnly(consitionTags);
            _closingAttackPhase = CLOSED_ATTACK_PHASE.NONE;
        }

        /// <summary>
        /// 近接攻撃シーケンスを開始します
        /// </summary>
        public void StartAttack()
        {
            _character.IsAttacked   = false;
            _closingAttackPhase     = CLOSED_ATTACK_PHASE.CLOSINGE;
            _character.ResetElapsedTime();

            _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.MOVE, true);
        }

        /// <summary>
        /// 近接攻撃時の流れを更新します
        /// </summary>
        /// <param name="departure">近接攻撃の開始地点</param>
        /// <param name="destination">近接攻撃の終了地点</param>
        /// <returns>終了判定</returns>
        public bool UpdateAttack(in Vector3 departure, in Vector3 destination)
        {
            var attackAnimtag = AttackAnimTags[_character.skillModifiedParam.AtkNum - 1];

            if (_character.GetBullet() != null) return false;

            float t = 0f;
            bool isReservedParry = _character.GetOpponentChara().IsSkillInUse(SkillsData.ID.SKILL_PARRY);

            switch (_closingAttackPhase)
            {
                case CLOSED_ATTACK_PHASE.CLOSINGE:
                    _character.ElapsedTime += Time.deltaTime;
                    t = Mathf.Clamp01(_character.ElapsedTime / Constants.ATTACK_CLOSING_TIME);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    _character.gameObject.transform.position = Vector3.Lerp(departure, destination, t);
                    if (1.0f <= t)
                    {
                        _character.ResetElapsedTime();
                        _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.MOVE, false);
                        _character.AnimCtrl.SetAnimator(attackAnimtag);

                        _closingAttackPhase = CLOSED_ATTACK_PHASE.ATTACK;
                    }
                    break;

                case CLOSED_ATTACK_PHASE.ATTACK:
                    if (_character.IsEndAttackAnimSequence())
                    {
                        _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.WAIT);

                        _closingAttackPhase = CLOSED_ATTACK_PHASE.DISTANCING;
                    }
                    break;
                case CLOSED_ATTACK_PHASE.DISTANCING:
                    // 攻撃前の場所に戻る
                    _character.ElapsedTime += Time.deltaTime;
                    t = Mathf.Clamp01(_character.ElapsedTime / Constants.ATTACK_DISTANCING_TIME);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    _character.gameObject.transform.position = Vector3.Lerp(destination, departure, t);
                    if (1.0f <= t)
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