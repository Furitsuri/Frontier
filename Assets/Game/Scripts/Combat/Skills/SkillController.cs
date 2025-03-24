using Frontier.Battle;
using UnityEngine;

namespace Frontier.Combat
{
    public class SkillController : Controller
    {
        private ParrySkillController _parryCtrl = null;
        public ParrySkillController ParryController => _parryCtrl;

        void Awake()
        {
            _parryCtrl = gameObject.GetComponentInChildren<ParrySkillController>();
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