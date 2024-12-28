using Frontier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class SkillController : Controller
    {
        private SkillParryController _parryCtrl = null;
        public SkillParryController ParryController => _parryCtrl;

        void Awake()
        {
            _parryCtrl = gameObject.GetComponentInChildren<SkillParryController>();
            Debug.Assert( _parryCtrl != null );
        }

        /// <summary>
        /// ‰Šú‰»‚µ‚Ü‚·
        /// </summary>
        /// <param name="btlMgr"></param>
        public void Init( BattleManager btlMgr )
        {
            _parryCtrl.Init( btlMgr );
        }
    }
}