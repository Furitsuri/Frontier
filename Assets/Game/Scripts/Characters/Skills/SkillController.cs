using Frontier.Battle;
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
        /// 初期化します
        /// </summary>
        /// <param name="btlRtnCtrl"></param>
        public void Init( BattleRoutineController btlRtnCtrl )
        {
            _parryCtrl.Init( btlRtnCtrl );
        }
    }
}