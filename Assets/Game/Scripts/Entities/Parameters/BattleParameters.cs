using System;
using UnityEngine;

namespace Frontier.Entities
{
    [Serializable]
    public class BattleParameters
    {
        private TemporaryParameter _tmpParam;
        private ModifiedParameter _modifiedParam;
        private SkillModifiedParameter _skillModifiedParam;

        public ref TemporaryParameter TmpParam => ref _tmpParam;
        public ref ModifiedParameter ModifiedParam => ref _modifiedParam;
        public ref SkillModifiedParameter SkillModifiedParam => ref _skillModifiedParam;

        public void Setup()
        {
            _tmpParam.Setup();
        }

        public void Init()
        {
            _tmpParam.Init();
            _modifiedParam.Init();
            _skillModifiedParam.Init();
        }
    }
}