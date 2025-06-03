using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class UISystem : MonoBehaviour
    {
        private GeneralUISystem _generarlUi = null;
        private BattleUISystem _battleUi    = null;
#if UNITY_EDITOR
        private DebugUISystem _debugUi      = null;
#endif // UNITY_EDITOR

        public GeneralUISystem GeneralUi => _generarlUi;
        public BattleUISystem BattleUi => _battleUi;
#if UNITY_EDITOR
        public DebugUISystem DebugUi => _debugUi;
#endif // UNITY_EDITOR

        /// <summary>
        /// UIのカテゴリを示すインデックス値です
        /// </summary>
        enum ChildIndex
        {
            General = 0,
            Battle,
#if UNITY_EDITOR
            Debug,
#endif // UNITY_EDITOR
        }

        // Start is called before the first frame update
        void Awake()
        {
            Transform childGeneralUI = transform.GetChild( (int)ChildIndex.General );
            if( childGeneralUI != null )
            {
                _generarlUi = childGeneralUI.GetComponent<GeneralUISystem>();
            }
            Debug.Assert( _generarlUi != null );

            Transform childBattleUI = transform.GetChild( (int)ChildIndex.Battle );
            if( childBattleUI != null )
            {
                _battleUi = childBattleUI.GetComponent<BattleUISystem>();
            }
            Debug.Assert( _battleUi != null );

#if UNITY_EDITOR
            Transform childDebugUI = transform.GetChild((int)ChildIndex.Debug);
            if (childDebugUI != null)
            {
                _debugUi = childDebugUI.GetComponent<DebugUISystem>();
            }
            Debug.Assert(_debugUi != null);
#endif // UNITY_EDITOR
        }
    }
}