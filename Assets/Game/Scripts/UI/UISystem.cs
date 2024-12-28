using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class UISystem : MonoBehaviour
    {
        private GeneralUISystem _generarlUI = null;
        private BattleUISystem _battleUI = null;

        public BattleUISystem BattleUI => _battleUI;

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
                _generarlUI = childGeneralUI.GetComponent<GeneralUISystem>();
            }
            Debug.Assert( _generarlUI != null );

            Transform childBattleUI = transform.GetChild( (int)ChildIndex.Battle );
            if( childBattleUI != null )
            {
                _battleUI = childBattleUI.GetComponent<BattleUISystem>();
            }
            Debug.Assert( _battleUI != null );
        }
    }
}