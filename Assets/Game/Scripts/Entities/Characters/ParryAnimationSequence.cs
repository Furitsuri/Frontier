using Frontier.Battle;
using UnityEngine;
using Zenject;
using Frontier.Combat.Skill;

namespace Frontier.Entities
{
    public class ParryAnimationSequence : ICombatAnimationSequence
    {
        private Character _character;
        private BattleRoutineController _btlRtnCtrl = null;
        private PARRY_PHASE _parryPhase = PARRY_PHASE.NONE;

        [Inject]
        void Construct(BattleRoutineController btlRtnCtrl)
        {
            _btlRtnCtrl = btlRtnCtrl;
        }

        public void Init(Character character, AnimDatas.AnimeConditionsTag[] consitionTags)
        {
            _character = character;
        }

        /// <summary>
        /// �p���B�V�[�P���X���J�n���܂�
        /// </summary>
        public void StartSequence()
        {
            _parryPhase = PARRY_PHASE.EXEC_PARRY;

            _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.PARRY);
            _character.ResetElapsedTime();
            _character.GetTimeScale.SetTimeScale(0.1f);    // �^�C���X�P�[����x�����A�p���B�������X���[���[�V�����Ō�����
        }

        /// <summary>
        /// �p���B�V�[�P���X���X�V���܂�
        /// </summary>
        /// <param name="departure">�U���̊J�n�n�_</param>
        /// <param name="destination">�U���̏I���n�_</param>
        /// <returns>�I������</returns>
        public bool UpdateSequence(in Vector3 departure, in Vector3 destination)
        {
            bool isJustParry = false;

            switch (_parryPhase)
            {
                case PARRY_PHASE.EXEC_PARRY:
                    if (isJustParry)
                    {
                        _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.SINGLE_ATTACK);

                        _parryPhase = PARRY_PHASE.AFTER_ATTACK;
                    }
                    else
                    {
                        if (_character.AnimCtrl.IsEndAnimationOnConditionTag(AnimDatas.AnimeConditionsTag.PARRY))
                        {
                            _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.WAIT);

                            return true;
                        }
                    }
                    break;
                case PARRY_PHASE.AFTER_ATTACK:
                    break;
            }

            return false;
        }
    }
}