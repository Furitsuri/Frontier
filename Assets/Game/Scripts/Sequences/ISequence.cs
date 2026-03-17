using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Sequences
{
    public interface ISequence
    {
        public void Start();
        public void End();
        public bool Update();
    }
}