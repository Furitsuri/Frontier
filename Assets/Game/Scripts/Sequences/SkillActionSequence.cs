using Frontier.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Sequences
{
    public class SkillActionSequence : ISequence
    {
        private readonly SkillActionBase _skillAction = null;

        public void Start()
        {
            _skillAction.Start();
        }

        public void End()
        {
            _skillAction.End();
        }

        public bool Update()
        {
            return _skillAction.Update();
        }
    }
}