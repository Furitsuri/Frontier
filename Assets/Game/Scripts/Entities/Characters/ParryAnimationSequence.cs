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
        /// パリィシーケンスを開始します
        /// </summary>
        public void StartSequence()
        {
            _parryPhase = PARRY_PHASE.EXEC_PARRY;

            _character.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.PARRY);
            _character.ResetElapsedTime();
            _character.GetTimeScale.SetTimeScale(0.1f);    // タイムスケールを遅くし、パリィ挙動をスローモーションで見せる
        }

        /// <summary>
        /// パリィシーケンスを更新します
        /// </summary>
        /// <param name="departure">攻撃の開始地点</param>
        /// <param name="destination">攻撃の終了地点</param>
        /// <returns>終了判定</returns>
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