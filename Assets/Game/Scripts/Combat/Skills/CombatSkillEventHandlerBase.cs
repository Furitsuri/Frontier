using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Combat.Skill
{
    public class CombatSkillEventHandlerBase : MonoBehaviour, ICombatSkillHandler
    {
        virtual public void Init() { }
        virtual public void Exit() { }
        virtual public void Update() { }
        virtual public void LateUpdate() { }
        virtual public void FixedUpdate() { }
    }
}