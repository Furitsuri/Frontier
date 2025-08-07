using Frontier.Battle;
using UnityEngine;

namespace Frontier.Combat
{
    public class CombatSkillEventController : MonoBehaviour
    {
        private CombatSkillEventHandlerBase _currentSkillHandler = null;
        public CombatSkillEventHandlerBase CurrentSkillHandler => _currentSkillHandler;

        [SerializeField]
        [Header("発動時にイベントを発動させる戦闘スキルハンドラを設定してください")]
        private CombatSkillEventHandlerBase[] _combatSkillEventHandlers = null;

        private void Start()
        {
            // 登録されているハンドラを一度全て無効化します
            foreach ( var hdlr in _combatSkillEventHandlers )
            {
                hdlr.gameObject.SetActive( false );
            }
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="btlRtnCtrl">戦闘ルーチン操作クラス</param>
        public void Init()
        {
            if( _currentSkillHandler != null )
            {
                // 既にハンドラが登録されている場合は初期化を行う
                _currentSkillHandler.Init();
            }
        }

        /// <summary>
        /// 戦闘スキルハンドラを登録します
        /// </summary>
        public void Register<T>() where T : CombatSkillEventHandlerBase
        {
            for( int i = 0; i < _combatSkillEventHandlers.Length; ++i )
            {
                if( _combatSkillEventHandlers[i] is T )
                {
                    _currentSkillHandler = _combatSkillEventHandlers[i];
                    break;
                }
            }
        }

        public void Update()
        {             
            if ( _currentSkillHandler != null )
            {
                _currentSkillHandler.Update();
            }
        }

        public void LateUpdate()
        {
            if ( _currentSkillHandler != null )
            {
                _currentSkillHandler.LateUpdate();
            }
        }

        public void FixedUpdate()
        {
            if ( _currentSkillHandler != null )
            {
                _currentSkillHandler.FixedUpdate();
            }
        }
    }
}