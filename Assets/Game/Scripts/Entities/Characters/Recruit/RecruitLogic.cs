using Frontier.FormTroop;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities
{
    public sealed class RecruitLogic
    {
        private int _cost       = 0;
        private bool _isEmployed  = false;
        private Character _owner;

        public int Cost => _cost;
        public bool IsEmployed => _isEmployed;

        /// <summary>
        /// ステージレベルと、既にリストアップされている傭兵リストを基にして雇用候補となる傭兵を設定します
        /// </summary>
        /// <param name="stageLevel"></param>
        /// <param name="candidateMercenaries"></param>
        public void Setup( Character owner, int cost )
        {
            _owner      = owner;
            _isEmployed = false;
            _cost       = cost;
        }

        public void Dispose()
        {
            _owner = null;
        }

        public void SetEmployed( bool isEmployed )
        {
            _isEmployed = isEmployed;
        }
    }
}