using UnityEngine;

namespace Frontier.Entities
{
    public interface ICombatAnimationSequence
    {
        public void Init( Character character, AnimDatas.AnimeConditionsTag[] consitionTags );
        public void StartSequence();
        public bool UpdateSequence(in Vector3 departure, in Vector3 destination);
    }
}