using Frontier.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Combat
{
    public class SkillActionBase
    {
        protected Character _owner = null;

        public SkillActionBase( Character owner )
        {
            _owner = owner;
        }

        protected virtual void StartAction()
        {
        }

        protected virtual void UpdateAction()
        {
        }

        protected virtual void EndAction()
        {
        }
    }
}