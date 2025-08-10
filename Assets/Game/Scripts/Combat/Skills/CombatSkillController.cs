using Frontier.Battle;
using UnityEngine;

namespace Frontier.Combat
{
    public class CombatSkillEventController : FocusRoutineBase
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

        #region IFocusRoutine Implementation

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="btlRtnCtrl">戦闘ルーチン操作クラス</param>
        override public void Init()
        {
            if( _currentSkillHandler != null )
            {
                // 既にハンドラが登録されている場合は初期化を行う
                _currentSkillHandler.Init();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        override public int GetPriority()
        {
            return (int)FocusRoutinePriority.BATTLE_SKILL_EVENT;
        }

        #endregion

        /// <summary>
        /// 戦闘スキルハンドラを登録します
        /// </summary>
        /// <typeparam name="T">登録対象の型</typeparam>
        public void Register<T>() where T : CombatSkillEventHandlerBase
        {
            for( int i = 0; i < _combatSkillEventHandlers.Length; ++i )
            {
                if( _combatSkillEventHandlers[i] is T )
                {
                    _currentSkillHandler = _combatSkillEventHandlers[i];
                    return;
                }
            }

            LogHelper.LogError( $"CombatSkillEventController: Register failed. {typeof(T).Name} not found in handlers." );
        }

        /// <summary>
        /// 戦闘スキルハンドラを登録解除します
        /// </summary>
        /// <typeparam name="T">登録解除対象の型</typeparam>
        public void Unregister<T>() where T : CombatSkillEventHandlerBase
        {
            if( _currentSkillHandler is T )
            {
                _currentSkillHandler = null;
            }
            else
            {
                LogHelper.LogError( $"CombatSkillEventController: Unregister failed. {typeof(T).Name} is not the current handler." );
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