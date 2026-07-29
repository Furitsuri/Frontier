using UnityEngine;
using Frontier.Entities;

namespace Frontier.Combat
{
    public interface ICombatAnimationSequence
    {
        public void Init( Character character, AnimDatas.AnimeConditionsTag[] consitionTags );
        public void StartSequence();
        public bool UpdateSequence(in Vector3 departure, in Vector3 destination);
    }
}