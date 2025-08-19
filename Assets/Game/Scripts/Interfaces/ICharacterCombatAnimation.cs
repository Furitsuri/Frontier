using UnityEngine;

namespace Frontier.Entities
{
    public interface ICharacterCombatAnimation
    {
        public void Init( Character character, AnimDatas.AnimeConditionsTag[] consitionTags );

        public void StartAttack();

        public bool UpdateAttack(in Vector3 departure, in Vector3 destination);
    }
}