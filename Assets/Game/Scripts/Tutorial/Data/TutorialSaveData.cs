using System;
using System.Collections.Generic;

namespace Frontier.Tutorial
{
    [Serializable]
    public class TutorialSaveData
    {
        public List<TriggerType> _shownTriggers = new();
    }
}