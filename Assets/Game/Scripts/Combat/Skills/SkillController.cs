using Frontier.Battle;
using UnityEngine;

namespace Frontier.Combat
{
    public class SkillController : MonoBehaviour
    {
        private ParrySkillHandler _parryHdlr = null;
        public ParrySkillHandler ParryHdlr => _parryHdlr;

        void Awake()
        {
            _parryHdlr = gameObject.GetComponentInChildren<ParrySkillHandler>();
            Debug.Assert( _parryHdlr != null );
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="btlRtnCtrl">戦闘ルーチン操作クラス</param>
        public void Init( BattleRoutineController btlRtnCtrl )
        {
            _parryHdlr.Init( btlRtnCtrl );
        }
    }
}