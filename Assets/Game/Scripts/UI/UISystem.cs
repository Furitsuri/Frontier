using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class UISystem : MonoBehaviour
    {
        private GeneralUISystem _generarlUi = null;
        private BattleUISystem _battleUi    = null;

        public GeneralUISystem GeneralUi => _generarlUi;
        public BattleUISystem BattleUi => _battleUi;

        /// <summary>
        /// UIのカテゴリを示すインデックス値です
        /// </summary>
        enum ChildIndex
        {
            General = 0,
            Battle,
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
        }
    }
}