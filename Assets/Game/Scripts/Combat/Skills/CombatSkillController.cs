using Frontier.Battle;
using UnityEngine;

namespace Frontier.Combat
{
    public class CombatSkillController : MonoBehaviour
    {
        private ICombatSkillHandler _currentSkillHandler = null;
        public ICombatSkillHandler CurrentSkillHandler => _currentSkillHandler;

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="btlRtnCtrl">戦闘ルーチン操作クラス</param>
        public void Init()
        {
            _currentSkillHandler.Init();
        }

        /// <summary>
        /// 戦闘スキルハンドラを登録します
        /// </summary>
        /// <param name="hdlr">登録するハンドラ</param>
        public void Register( ICombatSkillHandler hdlr )
        {
            _currentSkillHandler = hdlr;
        }
    }
}