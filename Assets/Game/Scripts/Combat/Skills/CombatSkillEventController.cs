using Frontier.Battle;
using UnityEngine;
using Zenject;

namespace Frontier.Combat.Skill
{
    public class CombatSkillEventController : FocusRoutineBase
    {
        private HierarchyBuilderBase _hierarchyBld = null;
        private CombatSkillEventHandlerBase _currentSkillHandler = null;
        public CombatSkillEventHandlerBase CurrentSkillHandler => _currentSkillHandler;

        [SerializeField]
        [Header("スキル発動時にイベントを発動させる、戦闘スキルハンドラを設定してください")]
        private CombatSkillEventHandlerBase[] _combatSkillEventHdlrs = null;

        [Inject]
        void Construct( HierarchyBuilderBase hierarchyBld )
        {
            _hierarchyBld = hierarchyBld;
        }

        #region IFocusRoutine Implementation

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="btlRtnCtrl">戦闘ルーチン操作クラス</param>
        public override void Init()
        {
            if( _currentSkillHandler != null )
            {
                // 既にハンドラが登録されている場合は初期化を行う
                _currentSkillHandler.Init();
            }
        }

        public override void Exit()
        {
            Debug.Assert( _currentSkillHandler != null );

            _currentSkillHandler.Exit();

            base.Exit();
        }

        public override int GetPriority()
        {
            return (int)FocusRoutinePriority.BATTLE_SKILL_EVENT;
        }

        public override void UpdateRoutine()
        {
            if (_currentSkillHandler != null)
            {
                _currentSkillHandler.Update();
            }
        }

        public override void LateUpdateRoutine()
        {
            if (_currentSkillHandler != null)
            {
                _currentSkillHandler.LateUpdate();
            }
        }

        public override void FixedUpdateRoutine()
        {
            if (_currentSkillHandler != null)
            {
                _currentSkillHandler.FixedUpdate();
            }
        }

        #endregion

        /// <summary>
        /// 戦闘スキルハンドラを登録します
        /// </summary>
        /// <typeparam name="T">登録対象の型</typeparam>
        public void Register<T>() where T : CombatSkillEventHandlerBase
        {
            if( _currentSkillHandler != null )
            {
                Destroy( _currentSkillHandler );
            }

            for( int i = 0; i < _combatSkillEventHdlrs.Length; ++i )
            {
                if( _combatSkillEventHdlrs[i] is T )
                {
                    _currentSkillHandler = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<T>( _combatSkillEventHdlrs[i].gameObject, true, false, "" );
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
    }
}